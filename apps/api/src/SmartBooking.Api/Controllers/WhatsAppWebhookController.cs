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

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
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

                        if (messageType == "interactive" && message.TryGetProperty("interactive", out var interactive))
                        {
                            if (interactive.TryGetProperty("button_reply", out var btnReply) &&
                                btnReply.TryGetProperty("id", out var btnIdProp))
                            {
                                var buttonId = btnIdProp.GetString();
                                await HandleButtonReplyAsync(senderPhone, buttonId, cancellationToken);
                            }
                        }
                        else if (messageType == "button" && message.TryGetProperty("button", out var btnObj))
                        {
                            if (btnObj.TryGetProperty("payload", out var payloadProp))
                            {
                                var buttonId = payloadProp.GetString();
                                await HandleButtonReplyAsync(senderPhone, buttonId, cancellationToken);
                            }
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp Webhook işlenirken hata oluştu.");
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
    }
}