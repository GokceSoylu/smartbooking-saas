using SmartBooking.Application.DTOs;

namespace SmartBooking.Application.Interfaces;

public interface IAppointmentService
{
    Task<List<TimeSlotDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
}
