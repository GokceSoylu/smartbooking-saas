using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
