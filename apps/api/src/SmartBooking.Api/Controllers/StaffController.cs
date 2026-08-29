using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            .OrderBy(s => s.FullName)
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
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        _context.StaffMembers.Add(staff);
        await _context.SaveChangesAsync();

        return Ok(staff);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.Id == id);
        if (staff == null)
            return NotFound();

        staff.IsActive = false; // Soft delete
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateStaffRequest(string FullName, string Title, string PhoneNumber);