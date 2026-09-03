using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ISmartBookingDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;

    public TenantsController(ISmartBookingDbContext context, ICurrentTenantService currentTenantService)
    {
        _context = context;
        _currentTenantService = currentTenantService;
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLower());

        if (tenant == null || !tenant.IsActive)
            return NotFound(new { message = "İşletme bulunamadı." });

        return Ok(tenant);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllPublicTenants()
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Name, t.Slug, t.PhoneNumber })
            .ToListAsync();

        return Ok(tenants);
    }

    [HttpPatch("notification-settings")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateTenantNotificationRequest request)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue) return Unauthorized();

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value);
        if (tenant == null) return NotFound();

        tenant.NotifyOwnerOnNewAppointment = request.NotifyOwnerOnNewAppointment;
        await _context.SaveChangesAsync();

        return Ok(new { tenant.NotifyOwnerOnNewAppointment });
    }
}

public record UpdateTenantNotificationRequest(bool NotifyOwnerOnNewAppointment);