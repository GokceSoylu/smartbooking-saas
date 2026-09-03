using Microsoft.EntityFrameworkCore;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Service> Services { get; }
    DbSet<Staff> StaffMembers { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Appointment> Appointments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}