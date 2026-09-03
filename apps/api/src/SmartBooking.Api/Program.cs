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

// CORS Politikası (Geliştirme aşamasında her yerden istek alabilir)
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

// Canlı Meta WhatsApp Bildirim Servisi (Doğrudan HttpClient Factory ile)
builder.Services.AddHttpClient<INotificationService, MetaWhatsAppNotificationService>();

// Geriye dönük uyumluluk için (IWhatsAppService doğrudan kullanılıyorsa)
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

// No-Show Hatırlatıcı Arka Plan Servisi
builder.Services.AddHostedService<AppointmentReminderWorker>();

var app = builder.Build();

app.UseHttpsRedirection();

// CORS Middleware (Routing/Controller öncesinde çalışmalı)
app.UseCors();

// Tenant Çözümleme Middleware'i
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();