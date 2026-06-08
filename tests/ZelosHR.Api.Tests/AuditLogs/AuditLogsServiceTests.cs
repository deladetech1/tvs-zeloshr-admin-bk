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
}
