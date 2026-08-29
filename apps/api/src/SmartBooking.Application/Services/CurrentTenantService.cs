using SmartBooking.Application.Interfaces;

namespace SmartBooking.Application.Services;

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
