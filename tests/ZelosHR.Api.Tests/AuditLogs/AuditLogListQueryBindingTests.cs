using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Tests.AuditLogs;

/// <summary>Guards <c>EmployeeAuditLogParams</c> wire names on <see cref="AuditLogListQuery"/>.</summary>
public class AuditLogListQueryBindingTests
{
    public static TheoryData<string, string> EmployeeAuditLogParamNames => new()
    {
        { nameof(AuditLogListQuery.Page), PlatformQueryParams.Page },
        { nameof(AuditLogListQuery.Size), PlatformQueryParams.Size },
        { nameof(AuditLogFilterQuery.Search), PlatformQueryParams.Search },
        { nameof(AuditLogFilterQuery.Action), PlatformQueryParams.Action },
        { nameof(AuditLogFilterQuery.Severity), PlatformQueryParams.Severity },
        { nameof(AuditLogFilterQuery.Actor), PlatformQueryParams.Actor },
        { nameof(AuditLogFilterQuery.StartDate), PlatformQueryParams.StartDate },
        { nameof(AuditLogFilterQuery.EndDate), PlatformQueryParams.EndDate },
    };

    [Theory]
    [MemberData(nameof(EmployeeAuditLogParamNames))]
    public void EmployeeAuditLogParams_use_snake_case_FromQuery_names(string propertyName, string wireName)
    {
        var property = typeof(AuditLogListQuery).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            ?? typeof(AuditLogFilterQuery).GetProperty(propertyName);

        property.Should().NotBeNull($"property {propertyName} must exist on AuditLogListQuery");

        var fromQuery = property!.GetCustomAttribute<FromQueryAttribute>();
        fromQuery.Should().NotBeNull($"{propertyName} must have [FromQuery]");
        fromQuery!.Name.Should().Be(wireName);
    }
}
