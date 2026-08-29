using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.DTOs;
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
            .Select(s => new ServiceResponse(s.Id, s.Name, s.Description, s.DurationInMinutes, s.Price, s.IsActive))
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
            Price = request.Price
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return Ok(new ServiceResponse(service.Id, service.Name, service.Description, service.DurationInMinutes, service.Price, service.IsActive));
    }
}