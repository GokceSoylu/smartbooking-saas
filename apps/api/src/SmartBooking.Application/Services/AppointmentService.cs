using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ISmartBookingDbContext _context;
    private readonly INotificationService _notificationService;

    public AppointmentService(
        ISmartBookingDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<List<TimeSlotDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _context.Services
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service == null)
            throw new ArgumentException("Belirtilen hizmet bulunamadı.");

        var targetDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        var nextDay = targetDate.AddDays(1);
        var dayOfWeek = targetDate.DayOfWeek;

        var workingHour = await _context.WorkingHours
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.DayOfWeek == dayOfWeek, cancellationToken);

        if (workingHour != null && workingHour.IsClosed)
        {
            return new List<TimeSlotDto>();
        }

        var openTime = workingHour?.OpeningTime ?? new TimeSpan(9, 0, 0);
        var closeTime = workingHour?.ClosingTime ?? new TimeSpan(19, 0, 0);

        var workStart = targetDate.Date + openTime;
        var workEnd = targetDate.Date + closeTime;
        var slotDuration = TimeSpan.FromMinutes(service.DurationInMinutes);

        var existingAppointments = await _context.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.StaffId == request.StaffId &&
                        a.StartTimeUtc >= targetDate &&
                        a.StartTimeUtc < nextDay &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.Rejected)
            .ToListAsync(cancellationToken);

        var availableSlots = new List<TimeSlotDto>();
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
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service == null)
            throw new ArgumentException("Hizmet bulunamadı.");

        var staff = await _context.StaffMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);

        if (staff == null)
            throw new ArgumentException("Personel bulunamadı.");

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == service.TenantId, cancellationToken);

        if (tenant == null)
            throw new ArgumentException("İşletme bulunamadı.");

        var startTimeUtc = DateTime.SpecifyKind(request.StartTimeUtc, DateTimeKind.Utc);
        var endTimeUtc = startTimeUtc.AddMinutes(service.DurationInMinutes);
        var dayOfWeek = startTimeUtc.DayOfWeek;

        var workingHour = await _context.WorkingHours
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.DayOfWeek == dayOfWeek, cancellationToken);

        if (workingHour != null && workingHour.IsClosed)
            throw new InvalidOperationException("İşletme seçilen tarihte hizmet vermemektedir.");

        bool isBusy = await _context.Appointments
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.PhoneNumber == trimmedPhone && c.TenantId == service.TenantId, cancellationToken);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.CustomerFullName.Trim(),
                PhoneNumber = trimmedPhone,
                Notes = request.CustomerNotes,
                TenantId = service.TenantId
            };
            _context.Customers.Add(customer);
        }
        else
        {
            customer.FullName = request.CustomerFullName.Trim();
            customer.Notes = request.CustomerNotes;
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
            Status = AppointmentStatus.Pending,
            CustomerWantsWhatsAppNotification = true // Web sitesinden frontend ne yollarsa yollasın bildirim aktif
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // Hem müşteriye randevu_alindi hem işletmeye randevu_onay_talep gönderir
        await _notificationService.SendAppointmentRequestNotificationAsync(
            appointment,
            tenant,
            staff,
            service,
            customer,
            cancellationToken);

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

    public async Task<List<AppointmentResponse>> GetTenantAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Staff)
            .Include(a => a.Customer)
            .OrderByDescending(a => a.StartTimeUtc)
            .ToListAsync(cancellationToken);

        return appointments.Select(a => new AppointmentResponse(
            a.Id,
            a.TenantId,
            a.Service.Name,
            a.Staff.FullName,
            a.Customer != null ? a.Customer.FullName : "",
            a.Customer != null ? a.Customer.PhoneNumber : "",
            a.StartTimeUtc,
            a.EndTimeUtc,
            a.Price,
            a.Status
        )).ToList();
    }

    public async Task<AppointmentResponse> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .Include(a => a.Service)
            .Include(a => a.Staff)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            throw new ArgumentException("Randevu bulunamadı.");

        appointment.Status = newStatus;
        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == appointment.TenantId, cancellationToken);

        if (tenant != null && appointment.Customer != null)
        {
            await _notificationService.SendCustomerStatusUpdateAsync(appointment, appointment.Customer, tenant, cancellationToken);
        }

        return new AppointmentResponse(
            appointment.Id,
            appointment.TenantId,
            appointment.Service.Name,
            appointment.Staff.FullName,
            appointment.Customer != null ? appointment.Customer.FullName : "",
            appointment.Customer != null ? appointment.Customer.PhoneNumber : "",
            appointment.StartTimeUtc,
            appointment.EndTimeUtc,
            appointment.Price,
            appointment.Status
        );
    }
}