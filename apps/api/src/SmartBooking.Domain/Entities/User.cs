using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class User : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Owner"; // Owner, Staff, Admin, SuperAdmin
    public bool IsActive { get; set; } = true;
}