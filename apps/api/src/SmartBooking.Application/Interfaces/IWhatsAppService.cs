using SmartBooking.Domain.Entities;

namespace SmartBooking.Application.Interfaces;

public interface IWhatsAppService
{
    // İşletme sahibine butonlu onay/red mesajı atar
    Task SendAppointmentRequestNotificationAsync(Appointment appointment, Tenant tenant, Staff staff, Service service, Customer customer, CancellationToken cancellationToken = default);

    // Müşteriye durum güncellemesi (Onaylandı / Reddedildi) atar
    Task SendCustomerStatusUpdateAsync(Appointment appointment, Customer customer, Tenant tenant, CancellationToken cancellationToken = default);
}