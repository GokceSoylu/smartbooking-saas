using SmartBooking.Domain.Entities;

namespace SmartBooking.Application.Interfaces;

public interface IWhatsAppService
{
    // İşletme sahibine butonlu onay/red mesajı atar
    Task SendAppointmentRequestNotificationAsync(Appointment appointment, Tenant tenant, Staff staff, Service service, Customer customer, CancellationToken cancellationToken = default);

    // Müşteriye durum güncellemesi (Onaylandı / Reddedildi) atar
    Task SendCustomerStatusUpdateAsync(Appointment appointment, Customer customer, Tenant tenant, CancellationToken cancellationToken = default);
    // Randevuya 2 saat kala müşteriye hatırlatma mesajı atar
    Task SendAppointmentReminderAsync(Appointment appointment, Customer customer, Tenant tenant, CancellationToken cancellationToken = default);
}