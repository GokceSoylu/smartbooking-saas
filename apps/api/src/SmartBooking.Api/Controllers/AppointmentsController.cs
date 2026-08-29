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
        try
        {
            var result = await _appointmentService.CreateAppointmentAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}