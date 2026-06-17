using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Tests.AuditLogs;

public class AuditLogRepositoryFilterTests
{
    private const string TenantId = "tenant-1";
    private const string OrgId = "org-1";

    private static AuditLogRepository CreateRepository(out ZelosHrDbContext db)
    {
        db = new ZelosHrDbContext(
            new DbContextOptionsBuilder<ZelosHrDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var occurred = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        db.AuditLogs.AddRange(
            new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                OrgId = OrgId,
                OccurredAt = occurred,
                ActionTitle = "Personal information updated",
                ActorId = "u-admin-1",
                ActorFullName = "Demo Admin",
                EmployeeFullName = "Ama Mensah",
                Category = "Field change",
                Severity = "High",
                CreatedAt = occurred,
            },
            new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                OrgId = OrgId,
                OccurredAt = occurred.AddDays(-40),
                ActionTitle = "Employee created",
                ActorId = "u-hr-2",
                ActorFullName = "HR Manager",
                EmployeeFullName = "Kofi Boateng",
                Category = "Lifecycle",
                Severity = "Low",
                CreatedAt = occurred.AddDays(-40),
            });

        db.SaveChanges();
        return new AuditLogRepository(db);
    }

    [Fact]
    public async Task List_applies_severity_and_date_filters()
    {
        var repo = CreateRepository(out _);
        var query = new AuditLogListQuery
        {
            Page = 1,
            Size = 10,
            Severity = "High",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        };

        var (items, total) = await repo.ListScopedAsync(TenantId, OrgId, query, query.Page, query.Size);

        total.Should().Be(1);
        items.Should().ContainSingle(i =>
            i.ActionTitle == "Personal information updated"
            && i.Severity == "High"
            && i.ActorFullName == "Demo Admin");
    }

    [Fact]
    public async Task List_paginates_with_page_and_size()
    {
        var repo = CreateRepository(out var db);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 5; i++)
        {
            db.AuditLogs.Add(new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                OrgId = OrgId,
                OccurredAt = baseTime.AddDays(i),
                ActionTitle = $"Action {i}",
                ActorFullName = "Pager",
                Category = "System",
                Severity = "Low",
                CreatedAt = baseTime.AddDays(i),
            });
        }

        await db.SaveChangesAsync();

        var (items, total) = await repo.ListScopedAsync(
            TenantId,
            OrgId,
            new AuditLogListQuery { Page = 2, Size = 2 },
            page: 2,
            pageSize: 2);

        total.Should().Be(7);
        items.Should().HaveCount(2);
    }
}
