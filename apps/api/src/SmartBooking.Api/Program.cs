using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartBooking.Api.Middlewares;
using SmartBooking.Application.Interfaces;
using SmartBooking.Application.Services;
using SmartBooking.Infrastructure.BackgroundJobs;
using SmartBooking.Infrastructure.Persistence;
using SmartBooking.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller ve Keşif Servisleri
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. CORS Politikası (Dinamik Domain Doğrulamalı)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            string.IsNullOrEmpty(origin) ||
            origin.EndsWith("randevoapp.net") ||
            origin.StartsWith("http://localhost"))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. PostgreSQL & DbContext
builder.Services.AddDbContext<SmartBookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISmartBookingDbContext>(provider =>
    provider.GetRequiredService<SmartBookingDbContext>());

// 4. JWT Kimlik Doğrulama
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "super_secret_jwt_key_that_is_at_least_32_bytes_long!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartBookingApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SmartBookingClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// 5. Uygulama & Altyapı Servisleri
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// 6. WhatsApp ve Bildirim Servisleri
builder.Services.AddHttpClient<INotificationService, MetaWhatsAppNotificationService>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

// 7. Arka Plan Görevleri
builder.Services.AddHostedService<AppointmentReminderWorker>();

// 8. Reverse Proxy Header Yapılandırması
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Global Exception Handler (Hata anında da CORS başlıklarının korunmasını garanti eder)
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Unhandled Exception caught in pipeline.");

        await context.Response.WriteAsJsonAsync(new
        {
            error = exception?.Message ?? "Sunucu tarafında bir hata oluştu.",
            detail = exception?.StackTrace
        });
    });
});

app.UseForwardedHeaders();

// Otomatik Migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SmartBookingDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration uygulanırken hata oluştu.");
    }
}

// Middleware Sıralaması
app.UseRouting();
app.UseCors("AllowAllOrigins");

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();