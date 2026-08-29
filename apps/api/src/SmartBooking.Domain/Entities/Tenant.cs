using SmartBooking.Domain.Common;

namespace SmartBooking.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? WhatsAppPhoneNumberId { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
