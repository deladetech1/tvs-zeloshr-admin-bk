using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence;

public sealed class ZelosHrDbContext(DbContextOptions<ZelosHrDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<BranchEntity> Branches => Set<BranchEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<LifecycleEventEntity> LifecycleEvents => Set<LifecycleEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("zeloshr");

        modelBuilder.Entity<EmployeeEntity>(b =>
        {
            b.ToTable("zhr_employees");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeCode).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.GhanaCardNumber }).IsUnique();
            b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            b.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId);
        });

        modelBuilder.Entity<DepartmentEntity>(b =>
        {
            b.ToTable("zhr_departments");
            b.HasKey(x => x.Id);
            b.HasOne(x => x.ParentDepartment)
                .WithMany()
                .HasForeignKey(x => x.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HeadOfDepartment)
                .WithMany()
                .HasForeignKey(x => x.HeadOfDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BranchEntity>(b =>
        {
            b.ToTable("zhr_branches");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<AuditLogEntity>(b =>
        {
            b.ToTable("zhr_audit_logs");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<LifecycleEventEntity>(b =>
        {
            b.ToTable("zhr_lifecycle_events");
            b.HasKey(x => x.Id);
        });
    }
}
