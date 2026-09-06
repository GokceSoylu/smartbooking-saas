using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool NotifyOwnerOnNewAppointment { get; set; } = true;

    // Yönetici Onay ve Abonelik Kontrolleri
    public bool IsApproved { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? SubscriptionExpiresAtUtc { get; set; }
}