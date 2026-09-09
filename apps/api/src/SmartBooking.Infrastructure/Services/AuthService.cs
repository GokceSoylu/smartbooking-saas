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

        var tenant = new Tenant
        {
            Name = request.BusinessName.Trim(),
            Slug = normalizedSlug,
            PhoneNumber = request.PhoneNumber.Trim(),
            IsApproved = false,
            IsActive = true,
            SubscriptionExpiresAtUtc = null
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

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

        return new AuthResponse(
            token,
            user.FullName,
            user.Email,
            user.TenantId ?? Guid.Empty
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.ToLower().Trim();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");
        }

        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }
        catch
        {
            isPasswordValid = false;
        }

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");
        }

        // Admin veya SuperAdmin işletme doğrulama adımlarını atlar
        if (user.Role == "Admin" || user.Role == "SuperAdmin")
        {
            var adminToken = GenerateJwtToken(user);
            return new AuthResponse(adminToken, user.FullName, user.Email, user.TenantId ?? Guid.Empty);
        }

        if (user.TenantId.HasValue && user.TenantId.Value != Guid.Empty)
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value, cancellationToken);

            if (tenant == null)
            {
                throw new UnauthorizedAccessException("Kullanıcıya bağlı bir işletme kaydı bulunamadı.");
            }

            if (!tenant.IsApproved)
            {
                throw new UnauthorizedAccessException("İşletme hesabınız henüz yönetici onayından geçmemiştir. Lütfen onay bekleyin.");
            }

            if (!tenant.IsActive)
            {
                throw new UnauthorizedAccessException("İşletmenizin sistem erişimi geçici olarak durdurulmuştur.");
            }

            if (tenant.SubscriptionExpiresAtUtc.HasValue && tenant.SubscriptionExpiresAtUtc.Value < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Abonelik süreniz sona ermiştir. Lütfen aboneliğinizi yenileyiniz.");
            }
        }

        var token = GenerateJwtToken(user);
        return new AuthResponse(token, user.FullName, user.Email, user.TenantId ?? Guid.Empty);
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
            new Claim("tenant_id", user.TenantId?.ToString() ?? string.Empty),
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