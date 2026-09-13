using Microsoft.AspNetCore.Mvc;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    [HttpPost("available-slots")]
    public async Task<IActionResult> GetAvailableSlots([FromBody] GetAvailableSlotsRequest request)
    {
        var slots = await _appointmentService.GetAvailableSlotsAsync(request);
        return Ok(slots);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Cloudflare veya reverse proxy arkasındaki gerçek istemci IP adresini yakalama
            var clientIp = HttpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                           ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                           ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            // Gelen isteği yakalanan IP ile zenginleştirme (ispat yükümlülüğü logu)
            var finalRequest = request with { ClientIpAddress = clientIp };

            _logger.LogInformation(">>> [Randevu Talebi Geldi] Telefon: {Phone}, Başlangıç: {Start}, IP: {Ip}",
                finalRequest.CustomerPhoneNumber, finalRequest.StartTimeUtc, clientIp);

            var result = await _appointmentService.CreateAppointmentAsync(finalRequest, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> [Randevu Oluşturma Hatası]: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _appointmentService.GetTenantAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _appointmentService.UpdateAppointmentStatusAsync(id, request.Status, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> [Durum Güncelleme Hatası]: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    public record UpdateStatusRequest(SmartBooking.Domain.Enums.AppointmentStatus Status);
}