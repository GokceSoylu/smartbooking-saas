using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class WorkingHour : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DayOfWeek DayOfWeek { get; set; } // 0: Sunday, 1: Monday, ...
    public TimeSpan OpeningTime { get; set; } = new(9, 0, 0); // 09:00
    public TimeSpan ClosingTime { get; set; } = new(19, 0, 0); // 19:00
    public bool IsClosed { get; set; } = false; // Tatil günü mü?
}