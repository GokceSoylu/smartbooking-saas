namespace SmartBooking.Domain.Common;

public interface ITenantEntity
{
    public Guid TenantId { get; set; }
}
