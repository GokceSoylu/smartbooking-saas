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
        // 1. HTTP OPTIONS (CORS Preflight) isteklerini direkt geç
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // 2. Admin, Auth, Webhook ve Swagger endpoint'leri tenant filtresine tabi tutulmaz
        if (path.StartsWith("/api/admin") ||
            path.StartsWith("/api/auth") ||
            path.StartsWith("/api/webhook") ||
            path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // Header kontrolü (X-Tenant-Id: GUID)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            Guid.TryParse(tenantHeader, out var tenantId))
        {
            currentTenantService.SetTenant(tenantId);
        }

        await _next(context);
    }
}