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
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var nowUtc = DateTime.UtcNow;
        var reminderWindowEnd = nowUtc.AddHours(2);

        var upcomingAppointments = await context.Appointments
            .Include(a => a.Customer)
            .Where(a => a.Status == AppointmentStatus.Confirmed &&
                        !a.ReminderSent &&
                        a.StartTimeUtc > nowUtc &&
                        a.StartTimeUtc <= reminderWindowEnd)
            .ToListAsync(cancellationToken);

        foreach (var appointment in upcomingAppointments)
        {
            if (appointment.Customer != null && appointment.CustomerWantsWhatsAppNotification)
            {
                var tenant = await context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == appointment.TenantId, cancellationToken);

                if (tenant != null)
                {
                    await notificationService.SendAppointmentReminderAsync(
                        appointment,
                        appointment.Customer,
                        tenant,
                        cancellationToken);
                }
            }

            appointment.ReminderSent = true;
        }

        if (upcomingAppointments.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}