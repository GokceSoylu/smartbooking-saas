using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly ISmartBookingDbContext _context;

    public ServicesController(ISmartBookingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _context.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(services);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        var service = new Service
        {
            Name = request.Name,
            Description = request.Description,
            DurationInMinutes = request.DurationInMinutes,
            Price = request.Price,
            IsActive = true
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return Ok(service);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
        if (service == null)
            return NotFound();

        service.IsActive = false; // Soft delete
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateServiceRequest(string Name, string Description, int DurationInMinutes, decimal Price);