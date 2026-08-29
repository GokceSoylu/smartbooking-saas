using SmartBooking.Application.Interfaces;

namespace SmartBooking.Api.Middlewares;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService currentTenantService)
    {
        // 1. Önce Header kontrolü (X-Tenant-Id: GUID)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            Guid.TryParse(tenantHeader, out var tenantId))
        {
            currentTenantService.SetTenant(tenantId);
        }

        await _next(context);
    }
}