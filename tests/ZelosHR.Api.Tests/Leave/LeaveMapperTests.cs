using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Leave;

namespace ZelosHR.Api.Tests.Leave;

public class LeaveMapperTests
{
    private static readonly Guid EmployeeId = Guid.Parse("3804deee-d6ee-4b05-9efc-6e8ccf3b5ae3");
    private static readonly Guid LeaveTypeId = Guid.Parse("a2222222-2222-2222-2222-222222222203");

    private static readonly IReadOnlyDictionary<Guid, EmployeeLeaveContext> NoEmployees =
        new Dictionary<Guid, EmployeeLeaveContext>();

    private static readonly IReadOnlyDictionary<Guid, string> NoLeaveTypes =
        new Dictionary<Guid, string>();

    private static readonly IReadOnlyDictionary<string, string> NoUserNames =
        new Dictionary<string, string>();

    private static readonly DateTimeOffset SampleAuditAt =
        DateTimeOffset.Parse("2026-06-10T09:15:00+00:00");

    private static readonly IReadOnlyDictionary<Guid, DocumentReadDto?> NoProfileUrls =
        new Dictionary<Guid, DocumentReadDto?>();

    [Fact]
    public void MapBalance_uses_leave_type_lookup_when_id_matches()
    {
        var row = new LeaveBalanceRawRow(
            "bal-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            21,
            7,
            14,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var leaveTypes = new Dictionary<Guid, string>
        {
            [LeaveTypeId] = "Annual Leave",
        };

        var result = LeaveMapper.MapBalance(row, NoEmployees, leaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Annual Leave", result.LeaveType?.Name);
    }

    [Fact]
    public void MapBalance_falls_back_to_legacy_leave_type_name_when_lookup_missing()
    {
        var row = new LeaveBalanceRawRow(
            "bal-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            21,
            7,
            14,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapBalance(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Annual Leave", result.LeaveType?.Name);
    }

    [Fact]
    public void MapBalance_falls_back_to_employee_full_name_when_context_missing()
    {
        var row = new LeaveBalanceRawRow(
            "bal-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            21,
            7,
            14,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapBalance(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Jane Doe", result.Employee?.FullName);
        Assert.Equal(EmployeeId.ToString(), result.Employee?.EmployeeId);
    }

    [Fact]
    public void MapBalance_prefers_employee_context_over_legacy_name()
    {
        var row = new LeaveBalanceRawRow(
            "bal-1",
            EmployeeId.ToString(),
            "Legacy Name",
            LeaveTypeId.ToString(),
            "Annual Leave",
            21,
            7,
            14,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var employees = new Dictionary<Guid, EmployeeLeaveContext>
        {
            [EmployeeId] = new EmployeeLeaveContext(
                EmployeeId,
                "Jane Doe",
                "EMP-001",
                null,
                null,
                null,
                null,
                null,
                null),
        };

        var result = LeaveMapper.MapBalance(row, employees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Jane Doe", result.Employee?.FullName);
        Assert.Equal("EMP-001", result.Employee?.EmployeeCode);
    }

    [Fact]
    public void MapRequest_falls_back_to_employee_full_name_when_context_missing()
    {
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            5,
            "Pending",
            "pending_line_manager",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.Parse("2026-06-10T09:15:00+00:00"),
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Jane Doe", result.Employee?.FullName);
        Assert.Equal("Annual Leave", result.LeaveType?.Name);
    }

    [Fact]
    public void MapRequest_falls_back_to_approver_name_when_cp_lookup_missing()
    {
        const string approverId = "platform-user-99";
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            5,
            "Approved",
            "approved",
            approverId,
            "Bright Debrah",
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.Parse("2026-06-10T09:15:00+00:00"),
            DateTimeOffset.Parse("2026-06-11T16:20:00+00:00"),
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Bright Debrah", result.Approver?.FullName);
        Assert.Equal(approverId, result.Approver?.ApproverId);
    }

    [Fact]
    public void MergeLeaveTypeLookups_adds_legacy_name_when_type_missing_from_index()
    {
        var indexed = new Dictionary<Guid, string>();
        var rows = new[] { (LeaveTypeId.ToString(), "Annual Leave") };

        var merged = LeaveMapper.MergeLeaveTypeLookups(indexed, rows);

        Assert.True(merged.ContainsKey(LeaveTypeId));
        Assert.Equal("Annual Leave", merged[LeaveTypeId]);
    }

    [Fact]
    public void MapRequest_sets_returns_on_as_day_after_end_date()
    {
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            5,
            "Approved",
            "approved",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.Parse("2026-06-10T09:15:00+00:00"),
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal(new DateOnly(2026, 6, 15), result.ReturnsOn);
    }

    [Fact]
    public void MapRequest_uses_waiting_hours_when_no_prior_approval()
    {
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            5,
            "Pending",
            "pending_line_manager",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow.AddHours(-53),
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.NotNull(result.WaitingHours);
        Assert.Null(result.DaysSinceLastApproval);
    }

    [Fact]
    public void MapRequest_uses_days_since_last_approval_after_intermediate_approval()
    {
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Maternity",
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 30),
            30,
            "Pending",
            "pending_final",
            null,
            null,
            null,
            DateTimeOffset.UtcNow.AddDays(-5),
            null,
            DateTimeOffset.UtcNow.AddDays(-5),
            null,
            null,
            DateTimeOffset.UtcNow.AddDays(-10),
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.NotNull(result.DaysSinceLastApproval);
        Assert.Null(result.WaitingHours);
    }

    [Fact]
    public void MapRequest_resolves_leave_type_name_when_leave_type_id_empty()
    {
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            string.Empty,
            "Annual Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            5,
            "Pending",
            "pending_line_manager",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.Parse("2026-06-10T09:15:00+00:00"),
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal("Annual Leave", result.LeaveType?.Name);
    }

    [Fact]
    public void MapRequest_populates_audit_fields_with_resolved_display_names()
    {
        const string creatorId = "cp-user-creator";
        const string updaterId = "cp-user-updater";
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            5,
            "Pending",
            "pending_line_manager",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            SampleAuditAt,
            null,
            SampleAuditAt,
            SampleAuditAt.AddHours(1),
            creatorId,
            updaterId);

        var userNames = new Dictionary<string, string>
        {
            [creatorId] = "Ada Lovelace",
            [updaterId] = "Grace Hopper",
        };

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, userNames, NoProfileUrls);

        Assert.Equal(SampleAuditAt, result.CreatedAt);
        Assert.Equal(SampleAuditAt.AddHours(1), result.UpdatedAt);
        Assert.Equal(creatorId, result.CreatedById);
        Assert.Equal(updaterId, result.UpdatedById);
        Assert.Equal("Ada Lovelace", result.CreatedBy);
        Assert.Equal("Grace Hopper", result.UpdatedBy);
    }
}
