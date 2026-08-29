namespace SmartBooking.Application.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}
