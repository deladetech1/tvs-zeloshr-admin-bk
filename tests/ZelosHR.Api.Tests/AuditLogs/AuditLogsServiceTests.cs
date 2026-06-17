using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.AuditLogs;

namespace ZelosHR.Api.Tests.AuditLogs;

public class AuditLogsServiceTests
{
    private readonly IAuditLogRepository _repo = Substitute.For<IAuditLogRepository>();
    private readonly AuditLogsService _sut;

    public AuditLogsServiceTests() => _sut = new AuditLogsService(_repo);

    [Fact]
    public async Task GetById_returns_not_found_when_missing()
    {
        _repo.GetByIdScopedAsync(Arg.Any<Guid>(), "t1", "o1", Arg.Any<CancellationToken>())
            .Returns((AuditLogListRow?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetById_maps_actor_id()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new AuditLogListRow(
                id,
                DateTimeOffset.UtcNow,
                "Personal information updated",
                "Employee identity details updated.",
                Guid.NewGuid(),
                "EMP-0042",
                "Ama Mensah",
                "u1000001",
                "Demo Admin",
                "Field change",
                "Medium",
                false));

        var result = await _sut.GetByIdAsync(id, "t1", "o1");

        result.Success.Should().BeTrue();
        result.Data!.ActorId.Should().Be("u1000001");
        result.Data.ActorFullName.Should().Be("Demo Admin");
    }

    [Fact]
    public async Task List_passes_all_EmployeeAuditLogParams_to_repository()
    {
        var query = new AuditLogListQuery
        {
            Page = 2,
            Size = 15,
            Search = "admin",
            Action = "updated",
            Severity = "High",
            Actor = "u1",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
        };

        _repo.ListScopedAsync("t1", "o1", query, 2, 15, Arg.Any<CancellationToken>())
            .Returns(([], 0));
        _repo.GetSummaryScopedAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new AuditLogSummaryDto());

        var result = await _sut.ListAsync(query, "t1", "o1");

        result.Success.Should().BeTrue();
        await _repo.Received(1).ListScopedAsync("t1", "o1", query, 2, 15, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_rejects_invalid_date_range()
    {
        var result = await _sut.ListAsync(
            new AuditLogListQuery { StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 1, 1) },
            "t1",
            "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        await _repo.DidNotReceive().ListScopedAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<AuditLogFilterQuery>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_passes_filters_to_repository()
    {
        var query = new AuditLogExportQuery
        {
            Search = "admin",
            Action = "updated",
            Severity = "High",
            Actor = "u1",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
        };
        _repo.ExportListScopedAsync("t1", "o1", query, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.ExportCsvAsync(query, "t1", "o1");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        await _repo.Received(1).ExportListScopedAsync("t1", "o1", query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Purge_deletes_entries_older_than_retention_window()
    {
        _repo.PurgeOlderThanScopedAsync(
                "t1",
                "o1",
                Arg.Is<DateTimeOffset>(d => d < DateTimeOffset.UtcNow.AddDays(-80)),
                Arg.Any<CancellationToken>())
            .Returns(7);

        var result = await _sut.PurgeOldAsync(
            new AuditLogPurgeQuery { RetentionWindow = 90 }, "t1", "o1");

        result.Success.Should().BeTrue();
        result.Data!.DeletedCount.Should().Be(7);
        result.Data.RetentionWindow.Should().Be(90);
        result.Data.CutoffBefore.Should().BeBefore(DateTimeOffset.UtcNow.AddDays(-80).AddDays(2));
    }

    [Fact]
    public async Task Purge_rejects_invalid_retention_window()
    {
        var result = await _sut.PurgeOldAsync(
            new AuditLogPurgeQuery { RetentionWindow = 30 }, "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors.Should().ContainKey("retention_window");
        await _repo.DidNotReceive().PurgeOlderThanScopedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewPurge_returns_eligible_count()
    {
        _repo.CountOlderThanScopedAsync(
                "t1",
                "o1",
                Arg.Is<DateTimeOffset>(d => d < DateTimeOffset.UtcNow.AddDays(-170)),
                Arg.Any<CancellationToken>())
            .Returns(847);

        var result = await _sut.PreviewPurgeAsync(
            new AuditLogPurgeQuery { RetentionWindow = 180 }, "t1", "o1");

        result.Success.Should().BeTrue();
        result.Data!.EligibleCount.Should().Be(847);
        result.Data.RetentionWindow.Should().Be(180);
    }
}
