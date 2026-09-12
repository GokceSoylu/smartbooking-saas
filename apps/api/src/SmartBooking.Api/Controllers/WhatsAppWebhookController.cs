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

        _logger.LogInformation(">>> [Meta Webhook GET] Verify Token İsteği: {Token}", token);

        if (mode == "subscribe" && token == expectedToken)
        {
            _logger.LogInformation(">>> Meta Webhook doğrulaması BAŞARILI.");
            return Content(challenge ?? string.Empty, "text/plain");
        }

        _logger.LogWarning(">>> Meta Webhook doğrulama geçersiz!");
        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        // Meta imza kontrolü (AppSecret tanımlı değilse bypass edilir)
        if (!IsValidMetaSignature(rawBody))
        {
            _logger.LogWarning(">>> Geçersiz Meta Webhook imzası saptandı.");
            return Unauthorized();
        }

        _logger.LogInformation(">>> [Meta Webhook POST Alındı]: {Body}", rawBody);

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

                        string? buttonPayload = null;
                        string? buttonText = null;
                        string? textBody = null;

                        // 1. Template Quick Reply Buton Yanıtı
                        if (messageType == "button" && message.TryGetProperty("button", out var btnObj))
                        {
                            if (btnObj.TryGetProperty("payload", out var pProp)) buttonPayload = pProp.GetString();
                            if (btnObj.TryGetProperty("text", out var tProp)) buttonText = tProp.GetString();
                        }
                        // 2. Interactive Buton Yanıtı
                        else if (messageType == "interactive" && message.TryGetProperty("interactive", out var interactive))
                        {
                            if (interactive.TryGetProperty("button_reply", out var btnReply))
                            {
                                if (btnReply.TryGetProperty("id", out var idProp)) buttonPayload = idProp.GetString();
                                if (btnReply.TryGetProperty("title", out var titleProp)) buttonText = titleProp.GetString();
                            }
                        }
                        // 3. Normal Metin Yanıtı
                        else if (messageType == "text" && message.TryGetProperty("text", out var textObj))
                        {
                            if (textObj.TryGetProperty("body", out var bodyProp)) textBody = bodyProp.GetString();
                        }

                        _logger.LogInformation(">>> [Webhook Ayrıştırma] Gönderen: {Phone} | Tip: {Type} | Payload: {Payload} | Buton Metni: {ButtonText}",
                            senderPhone, messageType, buttonPayload, buttonText);

                        // Payload doğrudan geldiyse (CONFIRM_... / REJECT_...)
                        if (!string.IsNullOrWhiteSpace(buttonPayload) && (buttonPayload.StartsWith("CONFIRM_") || buttonPayload.StartsWith("REJECT_")))
                        {
                            await HandleButtonReplyAsync(buttonPayload, cancellationToken);
                        }
                        // Meta payload yerine sadece buton yazısını ("Onayla" / "Reddet") döndürdüyse
                        else if (!string.IsNullOrWhiteSpace(buttonText) && !string.IsNullOrWhiteSpace(senderPhone))
                        {
                            await HandleFallbackBySenderPhoneAsync(senderPhone, buttonText, cancellationToken);
                        }
                        // İşletme direkt mesaj yazdıysa ("onay", "ret", "1", "2")
                        else if (!string.IsNullOrWhiteSpace(textBody) && !string.IsNullOrWhiteSpace(senderPhone))
                        {
                            await HandleFallbackBySenderPhoneAsync(senderPhone, textBody, cancellationToken);
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> [Webhook Hata] Webhook işlenirken istisna oluştu.");
            return Ok();
        }
    }

    private async Task HandleButtonReplyAsync(string buttonPayload, CancellationToken cancellationToken)
    {
        AppointmentStatus? targetStatus = null;
        string? idString = null;

        if (buttonPayload.StartsWith("CONFIRM_"))
        {
            idString = buttonPayload.Replace("CONFIRM_", "").Trim();
            targetStatus = AppointmentStatus.Confirmed;
        }
        else if (buttonPayload.StartsWith("REJECT_"))
        {
            idString = buttonPayload.Replace("REJECT_", "").Trim();
            targetStatus = AppointmentStatus.Rejected;
        }

        if (targetStatus.HasValue && Guid.TryParse(idString, out var appointmentId))
        {
            _logger.LogInformation(">>> [Buton ID Eşleşti] Randevu: {Id}, Hedef Statü: {Status}", appointmentId, targetStatus.Value);
            await UpdateStatusAndNotifyCustomerAsync(appointmentId, targetStatus.Value, cancellationToken);
        }
        else
        {
            _logger.LogWarning(">>> [Geçersiz Buton Payload]: {Payload}", buttonPayload);
        }
    }

    private async Task HandleFallbackBySenderPhoneAsync(string senderPhone, string textOrTitle, CancellationToken cancellationToken)
    {
        var cleanSenderPhone = FormatPhoneNumber(senderPhone);
        var input = textOrTitle.Trim().ToLowerInvariant();

        _logger.LogInformation(">>> [Telefon Bazlı Arama] Tel: {Phone}, Girdi: {Input}", cleanSenderPhone, input);

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.PhoneNumber != null && t.PhoneNumber.Contains(cleanSenderPhone), cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning(">>> Telefon numarasına ait işletme bulunamadı: {Phone}", cleanSenderPhone);
            return;
        }

        var latestPendingAppointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenant.Id && a.Status == AppointmentStatus.Pending)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestPendingAppointment == null)
        {
            _logger.LogWarning(">>> İşletmeye ait bekleyen randevu bulunamadı. TenantId: {TenantId}", tenant.Id);
            return;
        }

        AppointmentStatus? targetStatus = null;
        if (input.Contains("onay") || input == "1" || input == "evet")
        {
            targetStatus = AppointmentStatus.Confirmed;
        }
        else if (input.Contains("red") || input.Contains("ret") || input.Contains("iptal") || input == "2" || input == "hayır")
        {
            targetStatus = AppointmentStatus.Rejected;
        }

        if (targetStatus.HasValue)
        {
            await UpdateStatusAndNotifyCustomerAsync(latestPendingAppointment.Id, targetStatus.Value, cancellationToken);
        }
    }

    private async Task UpdateStatusAndNotifyCustomerAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
        {
            _logger.LogError(">>> Randevu bulunamadı: {Id}", appointmentId);
            return;
        }

        if (appointment.Status == newStatus)
        {
            _logger.LogInformation(">>> Randevu zaten {Status} durumunda.", newStatus);
            return;
        }

        appointment.Status = newStatus;
        appointment.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(">>> Randevu durumu veritabanında güncellendi! Randevu: {Id} -> {Status}", appointmentId, newStatus);

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

        // Production'da AppSecret boşsa güvenliği bozmamak için doğrulamayı geç
        if (string.IsNullOrWhiteSpace(appSecret)) return true;

        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
            return true;

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
        if (!digits.StartsWith("90") && digits.Length == 10) digits = "90" + digits;
        return digits;
    }
}