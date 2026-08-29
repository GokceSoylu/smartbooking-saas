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
    private readonly ISmartBookingDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        ISmartBookingDbContext context,
        IWhatsAppService whatsAppService,
        IConfiguration configuration,
        ILogger<WhatsAppWebhookController> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var verifyToken = _configuration["WhatsApp:VerifyToken"] ?? "smartbooking_secure_verify_token";

        if (mode == "subscribe" && token == verifyToken)
        {
            _logger.LogInformation("WhatsApp webhook başarıyla doğrulandı.");
            return Ok(challenge);
        }

        return Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement rawPayload)
    {
        _logger.LogInformation("Webhook Payload: {Json}", rawPayload.ToString());

        try
        {
            if (!rawPayload.TryGetProperty("entry", out var entryProp) || entryProp.GetArrayLength() == 0)
                return Ok(new { status = "ignored_no_entry" });

            var firstEntry = entryProp[0];
            if (!firstEntry.TryGetProperty("changes", out var changesProp) || changesProp.GetArrayLength() == 0)
                return Ok(new { status = "ignored_no_changes" });

            var valueProp = changesProp[0].GetProperty("value");
            if (!valueProp.TryGetProperty("messages", out var messagesProp) || messagesProp.GetArrayLength() == 0)
                return Ok(new { status = "ignored_no_messages" });

            var message = messagesProp[0];
            if (!message.TryGetProperty("interactive", out var interactiveProp))
                return Ok(new { status = "ignored_not_interactive" });

            if (!interactiveProp.TryGetProperty("button_reply", out var buttonReplyProp))
                return Ok(new { status = "ignored_no_button_reply" });

            var buttonId = buttonReplyProp.GetProperty("id").GetString();
            _logger.LogInformation("Tıklanan Buton ID: {ButtonId}", buttonId);

            if (string.IsNullOrWhiteSpace(buttonId))
                return Ok(new { status = "empty_button_id" });

            var isConfirm = buttonId.StartsWith("CONFIRM_");
            var isReject = buttonId.StartsWith("REJECT_");

            if (!isConfirm && !isReject)
                return Ok(new { status = "unrecognized_button" });

            var appointmentIdString = isConfirm
                ? buttonId.Replace("CONFIRM_", "")
                : buttonId.Replace("REJECT_", "");

            if (!Guid.TryParse(appointmentIdString, out var appointmentId))
                return BadRequest(new { message = "Geçersiz randevu kimliği." });

            var appointment = await _context.Appointments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return NotFound(new { message = "Randevu bulunamadı." });

            appointment.Status = isConfirm ? AppointmentStatus.Confirmed : AppointmentStatus.Rejected;
            await _context.SaveChangesAsync();

            var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == appointment.CustomerId);
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == appointment.TenantId);

            if (customer != null && tenant != null)
            {
                await _whatsAppService.SendCustomerStatusUpdateAsync(appointment, customer, tenant);
            }

            return Ok(new
            {
                success = true,
                appointmentId = appointment.Id,
                status = appointment.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook işleme hatası");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}