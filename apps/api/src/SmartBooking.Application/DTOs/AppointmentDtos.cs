using SmartBooking.Domain.Enums;

namespace SmartBooking.Application.DTOs;

public record GetAvailableSlotsRequest(
    Guid ServiceId,
    Guid StaffId,
    DateTime Date
);

public record TimeSlotDto(
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    bool IsAvailable
);

public record CreateAppointmentRequest(
    Guid ServiceId,
    Guid StaffId,
    DateTime StartTimeUtc,
    string CustomerFullName,
    string CustomerPhoneNumber,
    string? CustomerNotes
);

public record AppointmentResponse(
    Guid Id,
    Guid TenantId,
    string ServiceName,
    string StaffName,
    string CustomerFullName,
    string CustomerPhoneNumber,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    decimal Price,
    AppointmentStatus Status
);
