using Microsoft.AspNetCore.Mvc;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost("available-slots")]
    public async Task<IActionResult> GetAvailableSlots([FromBody] GetAvailableSlotsRequest request)
    {
        var slots = await _appointmentService.GetAvailableSlotsAsync(request);
        return Ok(slots);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var result = await _appointmentService.CreateAppointmentAsync(request);
        return Ok(result);
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _appointmentService.GetTenantAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var result = await _appointmentService.UpdateAppointmentStatusAsync(id, request.Status);
        return Ok(result);
    }
    public record UpdateStatusRequest(SmartBooking.Domain.Enums.AppointmentStatus Status);
}