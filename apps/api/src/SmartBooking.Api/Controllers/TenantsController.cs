using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ISmartBookingDbContext _context;

    public TenantsController(ISmartBookingDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request)
    {
        var slugClean = request.Slug.ToLower().Trim();
        var slugExists = await _context.Tenants.AnyAsync(t => t.Slug == slugClean);
        if (slugExists)
        {
            return BadRequest(new { message = "Bu slug zaten kullanımda." });
        }

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = slugClean,
            PhoneNumber = request.PhoneNumber,
            WhatsAppPhoneNumberId = request.WhatsAppPhoneNumberId,
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return Ok(new TenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.PhoneNumber, tenant.IsActive));
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var slugClean = slug.ToLower().Trim();
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slugClean && t.IsActive);

        if (tenant is null)
        {
            return NotFound(new { message = "İşletme bulunamadı." });
        }

        return Ok(new TenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.PhoneNumber, tenant.IsActive));
    }
}