using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.DTOs;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ISmartBookingDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        ISmartBookingDbContext context,
        IWhatsAppService whatsAppService,
        ILogger<AppointmentService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
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
        var dayOfWeek = targetDate.DayOfWeek;

        var workingHour = await _context.WorkingHours
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
        _logger.LogInformation(">>> [AppointmentService] CreateAppointmentAsync başladı. Tel: {Phone}, İstek WhatsApp: {Flag}", request.CustomerPhoneNumber, request.CustomerWantsWhatsAppNotification);

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
        var dayOfWeek = startTimeUtc.DayOfWeek;

        var workingHour = await _context.WorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.DayOfWeek == dayOfWeek, cancellationToken);

        if (workingHour != null && workingHour.IsClosed)
            throw new InvalidOperationException("İşletme seçilen tarihte hizmet vermemektedir.");

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
            CustomerWantsWhatsAppNotification = request.CustomerWantsWhatsAppNotification
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // 1. MÜŞTERİYE WHATSAPP BİLDİRİMİ
        if (appointment.CustomerWantsWhatsAppNotification)
        {
            try
            {
                _logger.LogInformation(">>> [AppointmentService] Müşteriye WhatsApp bildirim isteği gönderiliyor...");
                await _whatsAppService.SendAppointmentRequestNotificationAsync(
                    appointment, tenant, staff, service, customer, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [AppointmentService] Müşteri WhatsApp bildirim hatası oluştu.");
            }
        }
        else
        {
            _logger.LogWarning(">>> [AppointmentService] CustomerWantsWhatsAppNotification FALSE olduğu için Müşteriye bildirim tetiklenmedi!");
        }

        // 2. İŞLETME SAHİBİNE (TENANT) WHATSAPP BİLDİRİMİ
        if (!string.IsNullOrWhiteSpace(tenant.PhoneNumber))
        {
            try
            {
                _logger.LogInformation(">>> [AppointmentService] İşletme sahibine ({TenantPhone}) WhatsApp bildirim isteği gönderiliyor...", tenant.PhoneNumber);

                // Müşteriye gönderilen bildirime ek olarak işletme sahibine bilgilendirme mesajı tetikleniyor
                await _whatsAppService.SendBusinessNewAppointmentNotificationAsync(
                    appointment, tenant, staff, service, customer, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [AppointmentService] İşletme sahibi WhatsApp bildirim hatası oluştu.");
            }
        }
        else
        {
            _logger.LogWarning(">>> [AppointmentService] İşletme sahibinin telefon numarası (Tenants.PhoneNumber) boş olduğu için WhatsApp bildirimi gönderilemedi!");
        }

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
        _logger.LogInformation(">>> [AppointmentService] UpdateAppointmentStatusAsync başladı. Id: {Id}, NewStatus: {Status}", appointmentId, newStatus);

        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Staff)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            throw new ArgumentException("Randevu bulunamadı.");

        appointment.Status = newStatus;
        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == appointment.TenantId, cancellationToken);

        if (tenant != null && appointment.Customer != null && appointment.CustomerWantsWhatsAppNotification)
        {
            try
            {
                _logger.LogInformation(">>> [AppointmentService] WhatsApp durum güncelleme isteği gönderiliyor...");
                await _whatsAppService.SendCustomerStatusUpdateAsync(appointment, appointment.Customer, tenant, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [AppointmentService] WhatsApp güncelleme bildirim hatası.");
            }
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