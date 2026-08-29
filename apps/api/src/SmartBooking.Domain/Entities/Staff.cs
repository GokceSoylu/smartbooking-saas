using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class Staff : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
