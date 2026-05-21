using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence;

public sealed class ZelosHrDbContext(DbContextOptions<ZelosHrDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<CpUserEntity> CpUsers => Set<CpUserEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<BranchEntity> Branches => Set<BranchEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<LifecycleEventEntity> LifecycleEvents => Set<LifecycleEventEntity>();
    public DbSet<AttendanceRecordEntity> AttendanceRecords => Set<AttendanceRecordEntity>();
    public DbSet<LeaveRequestEntity> LeaveRequests => Set<LeaveRequestEntity>();
    public DbSet<LeaveBalanceEntity> LeaveBalances => Set<LeaveBalanceEntity>();
    public DbSet<JobPostingEntity> JobPostings => Set<JobPostingEntity>();
    public DbSet<OnboardingTaskEntity> OnboardingTasks => Set<OnboardingTaskEntity>();
    public DbSet<PerformanceReviewEntity> PerformanceReviews => Set<PerformanceReviewEntity>();
    public DbSet<DisciplinaryCaseEntity> DisciplinaryCases => Set<DisciplinaryCaseEntity>();
    public DbSet<EmployeeDocumentEntity> EmployeeDocuments => Set<EmployeeDocumentEntity>();
    public DbSet<EmployeeEducationEntity> EmployeeEducations => Set<EmployeeEducationEntity>();
    public DbSet<EmployeeCertificationEntity> EmployeeCertifications => Set<EmployeeCertificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("zeloshr");

        modelBuilder.Entity<CpUserEntity>(b =>
        {
            b.ToTable("cp_users", "core_platform", t => t.ExcludeFromMigrations());
            b.HasKey(x => new { x.Id, x.TenantId });
        });

        modelBuilder.Entity<EmployeeEntity>(b =>
        {
            b.ToTable("zhr_employees");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeCode).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.GhanaCardNumber })
                .IsUnique()
                .HasFilter("ghana_card_number IS NOT NULL AND ghana_card_number <> ''");
            b.HasIndex(x => new { x.TenantId, x.UserId })
                .IsUnique()
                .HasFilter("user_id IS NOT NULL");
            b.Property(x => x.GrossSalary).HasPrecision(18, 4);
            b.Property(x => x.AnnualizedCost).HasPrecision(18, 4);
            b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            b.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId);
            b.HasOne(x => x.ReportsTo).WithMany().HasForeignKey(x => x.ReportsToId);
            b.HasOne(x => x.DottedLineManager).WithMany().HasForeignKey(x => x.DottedLineManagerId);
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

        modelBuilder.Entity<AuditLogEntity>(b => { b.ToTable("zhr_audit_logs"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<LifecycleEventEntity>(b => { b.ToTable("zhr_lifecycle_events"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<AttendanceRecordEntity>(b => { b.ToTable("zhr_attendance_records"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<LeaveRequestEntity>(b => { b.ToTable("zhr_leave_requests"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<LeaveBalanceEntity>(b => { b.ToTable("zhr_leave_balances"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<JobPostingEntity>(b => { b.ToTable("zhr_job_postings"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<OnboardingTaskEntity>(b => { b.ToTable("zhr_onboarding_tasks"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<PerformanceReviewEntity>(b => { b.ToTable("zhr_performance_reviews"); b.HasKey(x => x.Id); });
        modelBuilder.Entity<DisciplinaryCaseEntity>(b => { b.ToTable("zhr_disciplinary_cases"); b.HasKey(x => x.Id); });

        modelBuilder.Entity<EmployeeDocumentEntity>(b =>
        {
            b.ToTable("zhr_employee_documents");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<EmployeeEducationEntity>(b =>
        {
            b.ToTable("zhr_employee_education");
            b.HasKey(x => x.Id);
            b.HasOne<EmployeeEntity>().WithMany().HasForeignKey(x => x.EmployeeId);
        });

        modelBuilder.Entity<EmployeeCertificationEntity>(b =>
        {
            b.ToTable("zhr_employee_certifications");
            b.HasKey(x => x.Id);
            b.HasOne<EmployeeEntity>().WithMany().HasForeignKey(x => x.EmployeeId);
        });
    }
}
