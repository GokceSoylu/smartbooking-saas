namespace SmartBooking.Application.DTOs;

public record CreateServiceRequest(string Name, string? Description, int DurationInMinutes, decimal Price);
public record ServiceResponse(Guid Id, string Name, string? Description, int DurationInMinutes, decimal Price, bool IsActive);

public record CreateStaffRequest(string FullName, string? Title, string? PhoneNumber);
public record StaffResponse(Guid Id, string FullName, string? Title, string? PhoneNumber, bool IsActive);
