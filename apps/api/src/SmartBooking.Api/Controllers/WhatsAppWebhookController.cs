using System.Security.Cryptography;
using System.Text;
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
        var expectedToken = _configuration["WhatsApp:VerifyToken"];

        if (string.IsNullOrEmpty(expectedToken))
        {
            _logger.LogError("WhatsApp VerifyToken konfigürasyonda tanımlı değil.");
            return Unauthorized();
        }

        if (mode == "subscribe" && token == expectedToken)
        {
            _logger.LogInformation("Meta Webhook doğrulaması başarılı.");
            return Content(challenge ?? string.Empty, "text/plain");
        }

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        // 1. Meta Webhook İmza Doğrulaması (Production Güvenliği)
        if (!IsValidMetaSignature(rawBody))
        {
            _logger.LogWarning("Geçersiz Meta Webhook imzası saptandı.");
            return Unauthorized();
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var payload = doc.RootElement;

            if (!payload.TryGetProperty("entry", out var entries)) return Ok();

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

                        string? buttonId = null;
                        string? textBody = null;

                        // Quick Reply Template Buton Yanıtı
                        if (messageType == "interactive" && message.TryGetProperty("interactive", out var interactive))
                        {
                            if (interactive.TryGetProperty("button_reply", out var btnReply) &&
                                btnReply.TryGetProperty("id", out var btnIdProp))
                            {
                                buttonId = btnIdProp.GetString();
                            }
                        }
                        // Standart Interactive Buton Yanıtı
                        else if (messageType == "button" && message.TryGetProperty("button", out var btnObj))
                        {
                            if (btnObj.TryGetProperty("payload", out var payloadProp))
                            {
                                buttonId = payloadProp.GetString();
                            }
                        }
                        // Düz Metin Yanıtı (İşletme mesaja direkt yanıt yazdıysa)
                        else if (messageType == "text" && message.TryGetProperty("text", out var textObj))
                        {
                            if (textObj.TryGetProperty("body", out var bodyProp))
                            {
                                textBody = bodyProp.GetString();
                            }
                        }

                        if (!string.IsNullOrEmpty(buttonId))
                        {
                            await HandleButtonReplyAsync(buttonId, cancellationToken);
                        }
                        else if (!string.IsNullOrEmpty(textBody) && !string.IsNullOrEmpty(senderPhone))
                        {
                            await HandleTextReplyAsync(senderPhone, textBody, cancellationToken);
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp Webhook işlenirken hata oluştu.");
            return Ok(); // Meta'nın anlamsız retry yapmaması için 200 OK dönüyoruz
        }
    }

    private async Task HandleButtonReplyAsync(string buttonId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(buttonId)) return;

        AppointmentStatus? targetStatus = null;
        string? idString = null;

        if (buttonId.StartsWith("CONFIRM_"))
        {
            idString = buttonId.Replace("CONFIRM_", "").Trim();
            targetStatus = AppointmentStatus.Confirmed;
        }
        else if (buttonId.StartsWith("REJECT_"))
        {
            idString = buttonId.Replace("REJECT_", "").Trim();
            targetStatus = AppointmentStatus.Rejected;
        }

        if (targetStatus.HasValue && Guid.TryParse(idString, out var appointmentId))
        {
            await UpdateStatusAndNotifyCustomerAsync(appointmentId, targetStatus.Value, cancellationToken);
        }
    }

    private async Task HandleTextReplyAsync(string senderPhone, string textBody, CancellationToken cancellationToken)
    {
        var cleanSenderPhone = FormatPhoneNumber(senderPhone);
        textBody = textBody.Trim().ToLowerInvariant();

        _logger.LogInformation(">>> [Webhook] İşletmeden metin yanıtı geldi. Tel: {Phone}, Mesaj: {Text}", cleanSenderPhone, textBody);

        // İşletmenin telefon numarasına ait son gelen Pending (Bekleyen) randevuyu bulalım
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.PhoneNumber != null && t.PhoneNumber.Contains(cleanSenderPhone), cancellationToken);

        if (tenant == null) return;

        var latestAppointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenant.Id && a.Status == AppointmentStatus.Pending)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAppointment == null) return;

        AppointmentStatus? targetStatus = null;

        if (textBody.Contains("onay") || textBody == "1" || textBody == "evet")
        {
            targetStatus = AppointmentStatus.Confirmed;
        }
        else if (textBody.Contains("ret") || textBody.Contains("iptal") || textBody == "2" || textBody == "hayır")
        {
            targetStatus = AppointmentStatus.Rejected;
        }

        if (targetStatus.HasValue)
        {
            await UpdateStatusAndNotifyCustomerAsync(latestAppointment.Id, targetStatus.Value, cancellationToken);
        }
    }

    private async Task UpdateStatusAndNotifyCustomerAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null) return;

        // Idempotency: Zaten aynı statüdeyse tekrar güncelleme ve bildirim atma
        if (appointment.Status == newStatus) return;

        appointment.Status = newStatus;
        appointment.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

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

    private bool IsValidMetaSignature(string rawBody)
    {
        var appSecret = _configuration["WhatsApp:AppSecret"];

        // Local geliştirme ortamında AppSecret tanımlanmamışsa doğrulamayı atla
        if (string.IsNullOrEmpty(appSecret)) return true;

        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
            return false;

        var signature = signatureHeader.ToString().Replace("sha256=", "");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var expectedHash = Convert.ToHexString(hash).ToLower();

        return string.Equals(signature, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private string FormatPhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0") && digits.Length == 11) digits = digits[1..];
        if (digits.Length == 10) digits = "90" + digits;
        return digits;
    }
}