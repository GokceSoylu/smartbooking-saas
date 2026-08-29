using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly ISmartBookingDbContext _context;

    public StaffController(ISmartBookingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var staff = await _context.StaffMembers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new StaffResponse(s.Id, s.FullName, s.Title, s.PhoneNumber, s.IsActive))
            .ToListAsync();

        return Ok(staff);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
    {
        var staff = new Staff
        {
            FullName = request.FullName,
            Title = request.Title,
            PhoneNumber = request.PhoneNumber
        };

        _context.StaffMembers.Add(staff);
        await _context.SaveChangesAsync();

        return Ok(new StaffResponse(staff.Id, staff.FullName, staff.Title, staff.PhoneNumber, staff.IsActive));
    }
}