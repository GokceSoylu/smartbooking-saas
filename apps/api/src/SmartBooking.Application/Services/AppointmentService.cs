using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ISmartBookingDbContext _context;

    public AppointmentService(ISmartBookingDbContext context)
    {
        _context = context;
    }

    public async Task<List<TimeSlotDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _context.Services.FindAsync(new object[] { request.ServiceId }, cancellationToken);
        if (service == null)
            throw new ArgumentException("Belirtilen hizmet bulunamadı.");

        var targetDate = request.Date.Date;
        var nextDay = targetDate.AddDays(1);

        var existingAppointments = await _context.Appointments
            .Where(a => a.StaffId == request.StaffId &&
                        a.StartTimeUtc >= targetDate &&
                        a.StartTimeUtc < nextDay &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.Rejected)
            .ToListAsync(cancellationToken);

        var availableSlots = new List<TimeSlotDto>();

        var workStart = targetDate.AddHours(9);
        var workEnd = targetDate.AddHours(18);
        var slotDuration = TimeSpan.FromMinutes(service.DurationInMinutes);

        var currentSlotStart = workStart;
        while (currentSlotStart + slotDuration <= workEnd)
        {
            var currentSlotEnd = currentSlotStart + slotDuration;

            bool isOverlapping = existingAppointments.Any(a =>
                (currentSlotStart >= a.StartTimeUtc && currentSlotStart < a.EndTimeUtc) ||
                (currentSlotEnd > a.StartTimeUtc && currentSlotEnd <= a.EndTimeUtc) ||
                (currentSlotStart <= a.StartTimeUtc && currentSlotEnd >= a.EndTimeUtc));

            availableSlots.Add(new TimeSlotDto(
                currentSlotStart,
                currentSlotEnd,
                !isOverlapping
            ));

            currentSlotStart = currentSlotEnd;
        }

        return availableSlots;
    }

    public async Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _context.Services.FindAsync(new object[] { request.ServiceId }, cancellationToken);
        if (service == null)
            throw new ArgumentException("Hizmet bulunamadı.");

        var staff = await _context.StaffMembers.FindAsync(new object[] { request.StaffId }, cancellationToken);
        if (staff == null)
            throw new ArgumentException("Personel bulunamadı.");

        var endTimeUtc = request.StartTimeUtc.AddMinutes(service.DurationInMinutes);

        bool isBusy = await _context.Appointments.AnyAsync(a =>
            a.StaffId == request.StaffId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.Rejected &&
            ((request.StartTimeUtc >= a.StartTimeUtc && request.StartTimeUtc < a.EndTimeUtc) ||
             (endTimeUtc > a.StartTimeUtc && endTimeUtc <= a.EndTimeUtc) ||
             (request.StartTimeUtc <= a.StartTimeUtc && endTimeUtc >= a.EndTimeUtc)),
            cancellationToken);

        if (isBusy)
            throw new InvalidOperationException("Seçilen saat dilimi doludur. Lütfen başka bir slot seçin.");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.CustomerPhoneNumber.Trim(), cancellationToken);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.CustomerFullName,
                PhoneNumber = request.CustomerPhoneNumber.Trim(),
                Notes = request.CustomerNotes
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var appointment = new Appointment
        {
            CustomerId = customer.Id,
            StaffId = staff.Id,
            ServiceId = service.Id,
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = endTimeUtc,
            Price = service.Price,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        return new AppointmentResponse(
            appointment.Id,
            appointment.TenantId,
            service.Name,
            staff.FullName,
            customer.FullName,
            customer.PhoneNumber,
            appointment.StartTimeUtc,
            appointment.EndTimeUtc,
            appointment.Price,
            appointment.Status
        );
    }
}
