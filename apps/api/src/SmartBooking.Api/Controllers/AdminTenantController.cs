using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.Interfaces;

namespace SmartBooking.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



[ApiController]
[Route("api/admin/tenants")]
[Authorize(Roles = "Admin")] // 🔒 Sadece Admin rolüne sahip JWT token'ı olanlar istek atabilir!
public class AdminTenantController : ControllerBase
{
    private readonly ISmartBookingDbContext _context;

    public AdminTenantController(ISmartBookingDbContext context)
    {
        _context = context;
    }

    // 1. Tüm işletmeleri ve durumlarını listele
    [HttpGet]
    public async Task<IActionResult> GetAllTenants(CancellationToken cancellationToken)
    {
        var tenants = await _context.Tenants
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.PhoneNumber,
                t.IsApproved,
                t.IsActive,
                t.SubscriptionExpiresAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    // 2. Yeni başvuran işletmeyi ONAYLA (Sisteme giriş izni ver)
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveTenant(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant == null) return NotFound("İşletme bulunamadı.");

        tenant.IsApproved = true;
        tenant.IsActive = true;
        // Varsayılan olarak 30 günlük deneme/abonelik ver
        tenant.SubscriptionExpiresAtUtc = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { Message = $"{tenant.Name} onaylandı ve 30 günlük abonelik başlatıldı." });
    }

    // 3. İstediğin işletmenin ERİŞİMİNİ KAPAT veya AÇ (Abonelik ödemeyenleri tek tıkla durdur)
    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleTenantStatus(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant == null) return NotFound("İşletme bulunamadı.");

        tenant.IsActive = !tenant.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var durum = tenant.IsActive ? "Aktif edildi (Giriş yapabilir)" : "Kapatıldı (Girişi engellendi)";
        return Ok(new { Message = $"{tenant.Name} durumu: {durum}" });
    }

    // 4. Abonelik Süresini Uzat (Örn: Ay bazında süre ekleme)
    [HttpPost("{id}/extend-subscription")]
    public async Task<IActionResult> ExtendSubscription(Guid id, [FromQuery] int days, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant == null) return NotFound("İşletme bulunamadı.");

        var currentExpiry = tenant.SubscriptionExpiresAtUtc ?? DateTime.UtcNow;
        if (currentExpiry < DateTime.UtcNow) currentExpiry = DateTime.UtcNow;

        tenant.SubscriptionExpiresAtUtc = currentExpiry.AddDays(days);
        tenant.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { Message = $"{tenant.Name} aboneliği {tenant.SubscriptionExpiresAtUtc:dd.MM.yyyy} tarihine kadar uzatıldı." });
    }
}