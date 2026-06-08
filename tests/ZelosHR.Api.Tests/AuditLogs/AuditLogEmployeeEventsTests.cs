using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Tests.AuditLogs;

public class AuditLogEmployeeEventsTests
{
    [Fact]
    public void ForCreate_finalised_includes_invite_description()
    {
        var auditEvent = AuditLogEmployeeEvents.ForCreate(isFinalised: true);

        auditEvent.ActionTitle.Should().Be("Employee record created");
        auditEvent.Category.Should().Be("Lifecycle");
        auditEvent.Severity.Should().Be("Low");
        auditEvent.ActionDescription.Should().Contain("invite");
    }

    [Fact]
    public void ForUpdate_termination_is_high_and_flagged()
    {
        var request = new UpdateEmployeeAggregateRequest
        {
            Employment = new EmployeeAggregateEmploymentDto { EmploymentStatus = "Terminated" },
        };

        var auditEvent = AuditLogEmployeeEvents.ForUpdate(
            request,
            Guid.Parse("3804deee-d6ee-4b05-9efc-6e8ccf3b5ae3"),
            "EMP-0042",
            "Ama Mensah");

        auditEvent.ActionTitle.Should().Be("Employment status changed");
        auditEvent.Severity.Should().Be("High");
        auditEvent.IsFlagged.Should().BeTrue();
        auditEvent.EmployeeDisplayCode.Should().Be("EMP-0042");
    }

    [Fact]
    public void ForUpdate_identity_is_field_change_medium()
    {
        var request = new UpdateEmployeeAggregateRequest
        {
            Identity = new EmployeeAggregateIdentityDto { FullName = "New Name" },
        };

        var auditEvent = AuditLogEmployeeEvents.ForUpdate(
            request,
            Guid.NewGuid(),
            "EMP-0001",
            "Old Name");

        auditEvent.ActionTitle.Should().Be("Personal information updated");
        auditEvent.Category.Should().Be("Field change");
        auditEvent.Severity.Should().Be("Medium");
    }
}
