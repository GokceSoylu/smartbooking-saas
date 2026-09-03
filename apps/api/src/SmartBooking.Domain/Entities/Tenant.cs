using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // İşletme sahibinin kendine WhatsApp bildirimi isteyip istemediği
    public bool NotifyOwnerOnNewAppointment { get; set; } = true;
}