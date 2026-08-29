using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkingHoursController : ControllerBase
{
    private readonly ISmartBookingDbContext _context;

    public WorkingHoursController(ISmartBookingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var hours = await _context.WorkingHours
            .AsNoTracking()
            .OrderBy(w => w.DayOfWeek)
            .ToListAsync();

        // Eğer veritabanında henüz kayıt yoksa varsayılan 7 günü oluşturup dönelim
        if (hours.Count == 0)
        {
            var defaultHours = new List<WorkingHour>();
            for (int i = 0; i < 7; i++)
            {
                var day = (DayOfWeek)i;
                defaultHours.Add(new WorkingHour
                {
                    DayOfWeek = day,
                    OpeningTime = new TimeSpan(9, 0, 0),
                    ClosingTime = new TimeSpan(19, 0, 0),
                    IsClosed = (day == DayOfWeek.Sunday) // Pazar varsayılan kapalı
                });
            }

            _context.WorkingHours.AddRange(defaultHours);
            await _context.SaveChangesAsync();
            return Ok(defaultHours.OrderBy(w => w.DayOfWeek));
        }

        return Ok(hours);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] List<UpdateWorkingHourItem> items)
    {
        foreach (var item in items)
        {
            var record = await _context.WorkingHours.FirstOrDefaultAsync(w => w.DayOfWeek == item.DayOfWeek);
            if (record != null)
            {
                record.OpeningTime = TimeSpan.Parse(item.OpeningTime);
                record.ClosingTime = TimeSpan.Parse(item.ClosingTime);
                record.IsClosed = item.IsClosed;
            }
            else
            {
                _context.WorkingHours.Add(new WorkingHour
                {
                    DayOfWeek = item.DayOfWeek,
                    OpeningTime = TimeSpan.Parse(item.OpeningTime),
                    ClosingTime = TimeSpan.Parse(item.ClosingTime),
                    IsClosed = item.IsClosed
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record UpdateWorkingHourItem(DayOfWeek DayOfWeek, string OpeningTime, string ClosingTime, bool IsClosed);