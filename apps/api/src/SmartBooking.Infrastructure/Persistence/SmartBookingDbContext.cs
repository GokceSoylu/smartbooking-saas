using Microsoft.EntityFrameworkCore;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Common;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Infrastructure.Persistence;

public class SmartBookingDbContext : DbContext, ISmartBookingDbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public SmartBookingDbContext(
        DbContextOptions<SmartBookingDbContext> options,
        ICurrentTenantService currentTenantService) : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Staff> StaffMembers => Set<Staff>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
    public DbSet<User> Users => Set<User>();
    public Guid? CurrentTenantId => _currentTenantService.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Multi-tenant Global Query Filters
        modelBuilder.Entity<Staff>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Service>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<WorkingHour>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Slug)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.TenantId, c.PhoneNumber });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenantService.TenantId.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = _currentTenantService.TenantId.Value;
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}