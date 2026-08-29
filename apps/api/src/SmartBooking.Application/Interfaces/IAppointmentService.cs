using SmartBooking.Application.DTOs;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Application.Interfaces;

public interface IAppointmentService
{
    Task<List<TimeSlotDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<List<AppointmentResponse>> GetTenantAppointmentsAsync(CancellationToken cancellationToken = default);
    Task<AppointmentResponse> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus newStatus, CancellationToken cancellationToken = default);
}