using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class Service : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationInMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
