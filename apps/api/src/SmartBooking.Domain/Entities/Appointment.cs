using SmartBooking.Domain.Common;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Domain.Entities;

public class Appointment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public bool CustomerWantsWhatsAppNotification { get; set; } = true;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public bool ReminderSent { get; set; } = false;
    public decimal Price { get; set; }
    public string? CancellationReason { get; set; }
}
