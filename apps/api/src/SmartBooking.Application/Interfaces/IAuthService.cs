namespace SmartBooking.Application.Interfaces;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string BusinessName, string Slug, string FullName, string Email, string Password, string PhoneNumber);
public record AuthResponse(string Token, string FullName, string Email, Guid TenantId);

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}