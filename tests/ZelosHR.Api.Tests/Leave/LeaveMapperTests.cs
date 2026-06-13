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

    private static readonly IReadOnlyDictionary<Guid, LeaveTypeRefDto> NoLeaveTypes =
        new Dictionary<Guid, LeaveTypeRefDto>();

    private static readonly IReadOnlyDictionary<string, string> NoApproverNames =
        new Dictionary<string, string>();

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
            14);

        var leaveTypes = new Dictionary<Guid, LeaveTypeRefDto>
        {
            [LeaveTypeId] = LeaveMapper.ToTypeRef(LeaveTypeId, "Annual Leave"),
        };

        var result = LeaveMapper.MapBalance(row, NoEmployees, leaveTypes, NoProfileUrls);

        Assert.Equal("Annual Leave", result.LeaveType?.Name);
        Assert.Equal(LeaveTypeId.ToString(), result.LeaveType?.LeaveTypeId);
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
            14);

        var result = LeaveMapper.MapBalance(row, NoEmployees, NoLeaveTypes, NoProfileUrls);

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
            14);

        var result = LeaveMapper.MapBalance(row, NoEmployees, NoLeaveTypes, NoProfileUrls);

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
            14);

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

        var result = LeaveMapper.MapBalance(row, employees, NoLeaveTypes, NoProfileUrls);

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
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoApproverNames, NoProfileUrls);

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
            DateTimeOffset.Parse("2026-06-11T16:20:00+00:00"));

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoApproverNames, NoProfileUrls);

        Assert.Equal("Bright Debrah", result.Approver?.FullName);
    }

    [Fact]
    public void MergeLeaveTypeLookups_adds_legacy_name_when_type_missing_from_index()
    {
        var indexed = new Dictionary<Guid, LeaveTypeRefDto>();
        var rows = new[] { (LeaveTypeId.ToString(), "Annual Leave") };

        var merged = LeaveMapper.MergeLeaveTypeLookups(indexed, rows);

        Assert.True(merged.ContainsKey(LeaveTypeId));
        Assert.Equal("Annual Leave", merged[LeaveTypeId].Name);
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
            null);

        var result = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, NoApproverNames, NoProfileUrls);

        Assert.Equal("Annual Leave", result.LeaveType?.Name);
    }
}
