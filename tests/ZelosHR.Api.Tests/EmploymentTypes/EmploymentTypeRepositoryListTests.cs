using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Tests.EmploymentTypes;

public class EmploymentTypeRepositoryListTests
{
    private const string TenantId = "tenant-1";
    private const string OrgId = "org-1";

    private static EmploymentTypeRepository CreateRepository(out ZelosHrDbContext db)
    {
        db = new ZelosHrDbContext(
            new DbContextOptionsBuilder<ZelosHrDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var now = DateTimeOffset.UtcNow;
        var fullTimeId = Guid.NewGuid();
        var partTimeId = Guid.NewGuid();

        db.EmploymentTypes.AddRange(
            new EmploymentTypeEntity
            {
                Id = fullTimeId,
                TenantId = TenantId,
                OrgId = OrgId,
                Name = "Full-time",
                IsSystemDefault = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new EmploymentTypeEntity
            {
                Id = partTimeId,
                TenantId = TenantId,
                OrgId = OrgId,
                Name = "Part-time",
                IsSystemDefault = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });

        db.Employees.AddRange(
            new EmployeeEntity
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-001",
                TenantId = TenantId,
                OrgId = OrgId,
                FullName = "Ama Mensah",
                LifecycleState = "active",
                LifecycleStatus = "active",
                EmploymentTypeId = fullTimeId,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new EmployeeEntity
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-002",
                TenantId = TenantId,
                OrgId = OrgId,
                FullName = "Kofi Boateng",
                LifecycleState = "active",
                LifecycleStatus = "active",
                EmploymentTypeId = fullTimeId,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new EmployeeEntity
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-003",
                TenantId = TenantId,
                OrgId = OrgId,
                FullName = "Yaw Asante",
                LifecycleState = "active",
                LifecycleStatus = "active",
                EmploymentTypeId = null,
                EmploymentType = "Legacy text only",
                CreatedAt = now,
                UpdatedAt = now,
            });

        db.SaveChanges();
        return new EmploymentTypeRepository(db);
    }

    [Fact]
    public async Task List_returns_types_with_employee_counts()
    {
        var repo = CreateRepository(out _);
        var query = new EmploymentTypeListQuery { Page = 1, Size = 50 };

        var (items, total) = await repo.ListScopedAsync(TenantId, OrgId, query, query.Page, query.Size);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Single(i => i.Name == "Full-time").EmployeeCount.Should().Be(2);
        items.Single(i => i.Name == "Part-time").EmployeeCount.Should().Be(0);
    }

    [Fact]
    public async Task List_sorts_by_employee_count()
    {
        var repo = CreateRepository(out _);
        var query = new EmploymentTypeListQuery
        {
            Page = 1,
            Size = 50,
            SortBy = "employees",
            SortOrder = "desc",
        };

        var (items, _) = await repo.ListScopedAsync(TenantId, OrgId, query, query.Page, query.Size);

        items.Select(i => i.Name).Should().ContainInOrder("Full-time", "Part-time");
    }
}
