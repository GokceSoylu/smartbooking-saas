namespace SmartBooking.Application.DTOs;

public record RegisterTenantRequest(
    string Name,
    string Slug,
    string PhoneNumber,
    string? WhatsAppPhoneNumberId
);

public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string PhoneNumber,
    bool IsActive
);
