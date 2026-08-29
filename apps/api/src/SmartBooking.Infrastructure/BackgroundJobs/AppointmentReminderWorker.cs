using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Infrastructure.BackgroundJobs;

public class AppointmentReminderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public AppointmentReminderWorker(
        IServiceProvider serviceProvider,
        ILogger<AppointmentReminderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Randevu Hatırlatıcı Arka Plan Servisi (No-Show Engine) başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUpcomingRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatma servisi çalışırken bir hata meydana geldi.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessUpcomingRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ISmartBookingDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var nowUtc = DateTime.UtcNow;
        var reminderWindowEnd = nowUtc.AddHours(2);

        // Arka plan işçisi tüm tenant'ları tarayacağı için tenant filtresini devre dışı bırakıyoruz
        var upcomingAppointments = await context.Appointments
            .IgnoreQueryFilters()
            .Where(a => a.Status == AppointmentStatus.Confirmed &&
                        !a.ReminderSent &&
                        a.StartTimeUtc > nowUtc &&
                        a.StartTimeUtc <= reminderWindowEnd)
            .ToListAsync(cancellationToken);

        if (upcomingAppointments.Count == 0)
            return;

        _logger.LogInformation("{Count} adet yaklaşan randevu için hatırlatma mesajı gönderiliyor...", upcomingAppointments.Count);

        foreach (var appointment in upcomingAppointments)
        {
            var customer = await context.Customers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == appointment.CustomerId, cancellationToken);

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == appointment.TenantId, cancellationToken);

            if (customer != null && tenant != null)
            {
                await whatsAppService.SendAppointmentReminderAsync(appointment, customer, tenant, cancellationToken);
                appointment.ReminderSent = true;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}