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

    // 1. Meta Webhook URL Doğrulama (GET)
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && token == expectedToken)
        {
            _logger.LogInformation("Meta Webhook doğrulaması başarılı.");
            return Ok(challenge);
        }

        _logger.LogWarning("Geçersiz Webhook doğrulama isteği. Token uyuşmadı.");
        return Forbid();
    }

    // 2. Meta Olaylarını Dinleme (POST) - Buton Tıklamaları ve Yanıtlar
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
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
                        var senderPhone = message.GetProperty("from").GetString();
                        var messageType = message.GetProperty("type").GetString();

                        // Buton tıklaması yanıtı (Onayla / Reddet)
                        if (messageType == "interactive")
                        {
                            var interactive = message.GetProperty("interactive");
                            var interactiveType = interactive.GetProperty("type").GetString();

                            if (interactiveType == "button_reply")
                            {
                                var buttonId = interactive.GetProperty("button_reply").GetProperty("id").GetString();
                                await HandleButtonReplyAsync(senderPhone, buttonId, cancellationToken);
                            }
                        }
                        // Düz metin yanıtı ("Evet" veya "İptal")
                        else if (messageType == "text")
                        {
                            var textBody = message.GetProperty("text").GetProperty("body").GetString();
                            await HandleTextReplyAsync(senderPhone, textBody, cancellationToken);
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook işlenirken hata oluştu.");
            return Ok();
        }
    }

    private async Task HandleButtonReplyAsync(string? phone, string? buttonId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(buttonId)) return;

        _logger.LogInformation("WhatsApp Buton Yanıtı: {Phone} - Buton ID: {ButtonId}", phone, buttonId);

        if (buttonId.StartsWith("CONFIRM_"))
        {
            var idString = buttonId.Replace("CONFIRM_", "");
            if (Guid.TryParse(idString, out var appointmentId))
            {
                await UpdateStatusAndNotifyCustomerAsync(appointmentId, AppointmentStatus.Confirmed, cancellationToken);
            }
        }
        else if (buttonId.StartsWith("REJECT_"))
        {
            var idString = buttonId.Replace("REJECT_", "");
            if (Guid.TryParse(idString, out var appointmentId))
            {
                await UpdateStatusAndNotifyCustomerAsync(appointmentId, AppointmentStatus.Rejected, cancellationToken);
            }
        }
        else if (buttonId.StartsWith("CANCEL_"))
        {
            var idString = buttonId.Replace("CANCEL_", "");
            if (Guid.TryParse(idString, out var appointmentId))
            {
                await UpdateStatusAndNotifyCustomerAsync(appointmentId, AppointmentStatus.Cancelled, cancellationToken);
            }
        }
    }

    private async Task HandleTextReplyAsync(string? phone, string? text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(text)) return;

        var cleanText = text.Trim().ToLowerInvariant();
        _logger.LogInformation("WhatsApp Mesaj Yanıtı: {Phone} - Metin: {Text}", phone, cleanText);

        var cleanPhone = phone.StartsWith("90") ? phone[2..] : phone;
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Tenant)
            .Where(a => a.Customer.PhoneNumber.EndsWith(cleanPhone) && a.Status == AppointmentStatus.Pending)
            .OrderByDescending(a => a.StartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment == null) return;

        if (cleanText == "evet" || cleanText == "onay")
        {
            await UpdateStatusAndNotifyCustomerAsync(appointment.Id, AppointmentStatus.Confirmed, cancellationToken);
        }
        else if (cleanText == "iptal" || cleanText == "hayır" || cleanText == "red")
        {
            await UpdateStatusAndNotifyCustomerAsync(appointment.Id, AppointmentStatus.Rejected, cancellationToken);
        }
    }

    private async Task UpdateStatusAndNotifyCustomerAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment != null)
        {
            appointment.Status = newStatus;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Randevu durumu güncellendi: {Id} -> {Status}", appointmentId, newStatus);

            // Randevu durumu güncellendiğinde müşteriye anında teyit bildirimi gönder
            if (appointment.Customer != null && appointment.Tenant != null)
            {
                await _notificationService.SendCustomerStatusUpdateAsync(
                    appointment,
                    appointment.Customer,
                    appointment.Tenant,
                    cancellationToken);
            }
        }
    }
}