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
}
