using Microsoft.EntityFrameworkCore;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Application.Interfaces;

public interface ISmartBookingDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Staff> StaffMembers { get; }
    DbSet<Service> Services { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Appointment> Appointments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
