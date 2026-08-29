using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ISmartBookingDbContext _context;
    private readonly IWhatsAppService _whatsAppService;

    public AppointmentService(ISmartBookingDbContext context, IWhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    public async Task<List<TimeSlotDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service == null)
            throw new ArgumentException("Belirtilen hizmet bulunamadı.");

        var targetDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        var nextDay = targetDate.AddDays(1);

        var existingAppointments = await _context.Appointments
            .AsNoTracking()
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
        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service == null)
            throw new ArgumentException("Hizmet bulunamadı.");

        var staff = await _context.StaffMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null)
            throw new ArgumentException("Personel bulunamadı.");

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == service.TenantId, cancellationToken);

        if (tenant == null)
            throw new ArgumentException("İşletme bulunamadı.");

        var startTimeUtc = DateTime.SpecifyKind(request.StartTimeUtc, DateTimeKind.Utc);
        var endTimeUtc = startTimeUtc.AddMinutes(service.DurationInMinutes);

        bool isBusy = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(a =>
                a.StaffId == request.StaffId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Rejected &&
                ((startTimeUtc >= a.StartTimeUtc && startTimeUtc < a.EndTimeUtc) ||
                 (endTimeUtc > a.StartTimeUtc && endTimeUtc <= a.EndTimeUtc) ||
                 (startTimeUtc <= a.StartTimeUtc && endTimeUtc >= a.EndTimeUtc)),
                cancellationToken);

        if (isBusy)
            throw new InvalidOperationException("Seçilen saat dilimi doludur. Lütfen başka bir slot seçin.");

        var trimmedPhone = request.CustomerPhoneNumber.Trim();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == trimmedPhone, cancellationToken);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.CustomerFullName,
                PhoneNumber = trimmedPhone,
                Notes = request.CustomerNotes,
                TenantId = service.TenantId
            };
            _context.Customers.Add(customer);
        }

        var appointment = new Appointment
        {
            Customer = customer,
            StaffId = staff.Id,
            ServiceId = service.Id,
            TenantId = service.TenantId,
            StartTimeUtc = startTimeUtc,
            EndTimeUtc = endTimeUtc,
            Price = service.Price,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // Arka planda WhatsApp bildirimini tetikle
        await _whatsAppService.SendAppointmentRequestNotificationAsync(appointment, tenant, staff, service, customer, cancellationToken);

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