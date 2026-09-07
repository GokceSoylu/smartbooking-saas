using Microsoft.EntityFrameworkCore;
using SmartBooking.Api.Middlewares;
using SmartBooking.Application.Interfaces;
using SmartBooking.Application.Services;
using SmartBooking.Infrastructure.BackgroundJobs;
using SmartBooking.Infrastructure.Persistence;
using SmartBooking.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Controller ve API Keşif Servisleri
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS Politikası
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// PostgreSQL & DbContext
builder.Services.AddDbContext<SmartBookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISmartBookingDbContext>(provider =>
    provider.GetRequiredService<SmartBookingDbContext>());

// Uygulama & Altyapı Servisleri
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Canlı Meta WhatsApp Bildirim Servisi (HttpClient Factory ile)
builder.Services.AddHttpClient<INotificationService, MetaWhatsAppNotificationService>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

// Arka Plan Görevleri
builder.Services.AddHostedService<AppointmentReminderWorker>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SmartBookingDbContext>(); // Kendi DbContext adın
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration uygulanırken hata oluştu.");
    }
}
// CORS tüm isteklerin en başında devreye girmeli
app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Tenant Çözümleme Middleware'i
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();