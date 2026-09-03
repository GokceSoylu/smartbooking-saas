using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ISmartBookingDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ISmartBookingDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.ToLower().Trim();

        bool emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
            throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");

        var normalizedSlug = request.Slug.ToLower().Trim();
        bool slugExists = await _context.Tenants
            .AnyAsync(t => t.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
            throw new InvalidOperationException("Bu işletme bağlantı adı (slug) zaten kullanımda.");

        // 1. Yeni İşletme Oluştur
        var tenant = new Tenant
        {
            Name = request.BusinessName.Trim(),
            Slug = normalizedSlug,
            PhoneNumber = request.PhoneNumber.Trim(),
            IsActive = true
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Varsayılan Çalışma Saatlerini Oluştur (Pazar kapalı)
        var defaultHours = new List<WorkingHour>();
        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)i;
            defaultHours.Add(new WorkingHour
            {
                TenantId = tenant.Id,
                DayOfWeek = day,
                OpeningTime = new TimeSpan(9, 0, 0),
                ClosingTime = new TimeSpan(19, 0, 0),
                IsClosed = (day == DayOfWeek.Sunday)
            });
        }
        _context.WorkingHours.AddRange(defaultHours);

        // 3. İşletme Sahibini (Owner) Oluştur
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            TenantId = tenant.Id,
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FullName = request.FullName.Trim(),
            Role = "Owner",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user);
        return new AuthResponse(token, user.FullName, user.Email, user.TenantId);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.ToLower().Trim();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Hesabınız pasif durumdadır.");
        }

        var token = GenerateJwtToken(user);
        return new AuthResponse(token, user.FullName, user.Email, user.TenantId);
    }

    private string GenerateJwtToken(User user)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "super_secret_jwt_key_that_is_at_least_32_bytes_long!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("name", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "SmartBookingApi",
            audience: _configuration["Jwt:Audience"] ?? "SmartBookingClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}