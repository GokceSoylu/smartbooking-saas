using Microsoft.EntityFrameworkCore;
using SmartBooking.Api.Middlewares;
using SmartBooking.Application.Interfaces;
using SmartBooking.Application.Services;
using SmartBooking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// PostgreSQL & DbContext
builder.Services.AddDbContext<SmartBookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ISmartBookingDbContext arayüz eşlemesi
builder.Services.AddScoped<ISmartBookingDbContext>(provider =>
    provider.GetRequiredService<SmartBookingDbContext>());

// Servis Kayıtları
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

var app = builder.Build();

app.UseHttpsRedirection();

// Tenant Middleware
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();