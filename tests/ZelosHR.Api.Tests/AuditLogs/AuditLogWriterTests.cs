using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Tests.AuditLogs;

public class AuditLogWriterTests
{
    private readonly IAuditLogRepository _repo = Substitute.For<IAuditLogRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly AuditLogWriter _sut;

    public AuditLogWriterTests() =>
        _sut = new AuditLogWriter(_repo, _cpUsers, _httpContextAccessor);

    [Fact]
    public async Task RecordAsync_resolves_actor_from_jwt_and_cp_user()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[TrovesuiteHttpContextKeys.UserId] = "u1000001";
        _httpContextAccessor.HttpContext.Returns(ctx);
        _cpUsers.GetByIdAsync("u1000001", "t1", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u1000001", "Demo Admin", "admin@example.com", null, true));

        var auditEvent = new AuditEvent(
            "Employee record created",
            "Draft employee record created.",
            "Lifecycle",
            "Low",
            Guid.NewGuid(),
            "EMP-0001",
            "Ada Lovelace");

        await _sut.RecordAsync("t1", "o1", auditEvent);

        await _repo.Received(1).AppendScopedAsync(
            "t1",
            "o1",
            Arg.Is<AuditLogAppendRow>(row =>
                row.ActorId == "u1000001"
                && row.ActorFullName == "Demo Admin"
                && row.ActionTitle == "Employee record created"
                && row.EmployeeDisplayCode == "EMP-0001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_without_user_uses_system_actor()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        await _sut.RecordAsync(
            "t1",
            "o1",
            new AuditEvent("Employee record updated", null, "Lifecycle", "Low"));

        await _repo.Received(1).AppendScopedAsync(
            "t1",
            "o1",
            Arg.Is<AuditLogAppendRow>(row =>
                row.ActorId == null && row.ActorFullName == "System"),
            Arg.Any<CancellationToken>());
    }
}
