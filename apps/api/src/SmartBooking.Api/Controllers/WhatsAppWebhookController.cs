using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/webhook/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppWebhookController> _logger;
    private readonly ISmartBookingDbContext _context;
    private readonly INotificationService _notificationService;

    public WhatsAppWebhookController(
        IConfiguration configuration,
        ILogger<WhatsAppWebhookController> logger,
        ISmartBookingDbContext context,
        INotificationService notificationService)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"] ?? "smartbooking_secure_verify_token";

        if (mode == "subscribe" && token == expectedToken)
        {
            _logger.LogInformation("Meta Webhook doğrulaması başarılı.");
            return Content(challenge ?? string.Empty, "text/plain");
        }

        _logger.LogWarning("Geçersiz Webhook doğrulama isteği. Token uyuşmadı. Beklenen: {Expected}, Gelen: {Received}", expectedToken, token);
        return Unauthorized(); // Forbid() yerine Unauthorized()
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Webhook Ham İstek Geldi: {Raw}", payload.ToString());

            if (!payload.TryGetProperty("entry", out var entries))
                return Ok();

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes)) continue;

                foreach (var change in changes.EnumerateArray())
                {
                    if (!change.TryGetProperty("value", out var value)) continue;
                    if (!value.TryGetProperty("messages", out var messages)) continue;

                    foreach (var message in messages.EnumerateArray())
                    {
                        var senderPhone = message.TryGetProperty("from", out var p) ? p.GetString() : null;
                        var messageType = message.TryGetProperty("type", out var t) ? t.GetString() : null;

                        // 1. Buton Tıklaması
                        if (messageType == "interactive" && message.TryGetProperty("interactive", out var interactive))
                        {
                            if (interactive.TryGetProperty("button_reply", out var btnReply) &&
                                btnReply.TryGetProperty("id", out var btnIdProp))
                            {
                                var buttonId = btnIdProp.GetString();
                                _logger.LogInformation("Tıklanan Buton: {BtnId} Gönderen: {Phone}", buttonId, senderPhone);
                                await HandleButtonReplyAsync(senderPhone, buttonId, cancellationToken);
                            }
                        }
                        // 2. Metin Yanıtı
                        else if (messageType == "text" && message.TryGetProperty("text", out var textObj))
                        {
                            if (textObj.TryGetProperty("body", out var bodyProp))
                            {
                                var textBody = bodyProp.GetString();
                                _logger.LogInformation("Gelen Metin: {Body} Gönderen: {Phone}", textBody, senderPhone);
                                await HandleTextReplyAsync(senderPhone, textBody, cancellationToken);
                            }
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp Webhook işlenirken istisna oluştu.");
            return Ok();
        }
    }

    private async Task HandleButtonReplyAsync(string? phone, string? buttonId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(buttonId)) return;

        if (buttonId.StartsWith("CONFIRM_"))
        {
            var idString = buttonId.Replace("CONFIRM_", "").Trim();
            if (Guid.TryParse(idString, out var appointmentId))
            {
                await UpdateStatusAndNotifyCustomerAsync(appointmentId, AppointmentStatus.Confirmed, cancellationToken);
            }
        }
        else if (buttonId.StartsWith("REJECT_"))
        {
            var idString = buttonId.Replace("REJECT_", "").Trim();
            if (Guid.TryParse(idString, out var appointmentId))
            {
                await UpdateStatusAndNotifyCustomerAsync(appointmentId, AppointmentStatus.Rejected, cancellationToken);
            }
        }
    }

    private async Task HandleTextReplyAsync(string? phone, string? text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(text)) return;

        var cleanText = text.Trim().ToLowerInvariant();
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Include(a => a.Customer)
            .Where(a => a.Status == AppointmentStatus.Pending)
            .OrderByDescending(a => a.StartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment == null)
        {
            _logger.LogWarning("Bekleyen randevu bulunamadı.");
            return;
        }

        if (cleanText is "evet" or "onay" or "onayla")
        {
            await UpdateStatusAndNotifyCustomerAsync(appointment.Id, AppointmentStatus.Confirmed, cancellationToken);
        }
        else if (cleanText is "iptal" or "hayır" or "red" or "reddet")
        {
            await UpdateStatusAndNotifyCustomerAsync(appointment.Id, AppointmentStatus.Rejected, cancellationToken);
        }
    }

    private async Task UpdateStatusAndNotifyCustomerAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment != null)
        {
            appointment.Status = newStatus;
            appointment.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Randevu Durumu Başarıyla Değişti -> ID: {Id} -> {Status}", appointmentId, newStatus);

            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == appointment.TenantId, cancellationToken);

            if (appointment.Customer != null && tenant != null)
            {
                await _notificationService.SendCustomerStatusUpdateAsync(
                    appointment,
                    appointment.Customer,
                    tenant,
                    cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Güncellenecek randevu ID ile eşleşmedi: {Id}", appointmentId);
        }
    }
}