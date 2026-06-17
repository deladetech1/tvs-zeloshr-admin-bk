using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Leave;

namespace ZelosHR.Api.Tests.Leave;

public class LeaveMapperTests
{
    private static readonly Guid EmployeeId = Guid.Parse("3804deee-d6ee-4b05-9efc-6e8ccf3b5ae3");
    private static readonly Guid LeaveTypeId = Guid.Parse("a2222222-2222-2222-2222-222222222203");
    private static readonly Guid MaternityLeaveTypeId = Guid.Parse("a2222222-2222-2222-2222-222222222204");

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

    [Fact]
    public void ToDetail_uses_waiting_hours_on_final_queue_even_after_lm_and_hod_approved()
    {
        var row = new LeaveRequestRawRow(
            "req-1",
            EmployeeId.ToString(),
            "Kwame Asare",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 6, 19),
            5,
            "Pending",
            "pending_final",
            null,
            null,
            "cp-user-lm",
            DateTimeOffset.UtcNow.AddDays(-6),
            "cp-user-hod",
            DateTimeOffset.UtcNow.AddDays(-5),
            "Family trip to Cape Coast. Handover completed.",
            11,
            DateTimeOffset.UtcNow.AddHours(-53),
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var approverNames = new Dictionary<string, string>
        {
            ["cp-user-lm"] = "Adwoa Bediako",
            ["cp-user-hod"] = "Esi Quainoo",
        };

        var listItem = LeaveMapper.MapRequest(row, NoEmployees, NoLeaveTypes, approverNames, NoProfileUrls);
        var detail = LeaveMapper.ToDetail(
            listItem,
            row,
            workingDays: 5,
            publicHolidaysInRange: 0,
            LeaveMapper.BuildBalanceImpact(row.RemainingDays, row.DaysRequested),
            approverNames);

        Assert.Null(listItem.WaitingHours);
        Assert.NotNull(detail.WaitingHours);
        Assert.True(detail.WaitingHours >= 53);
        Assert.Equal(3, detail.ApprovalTrail.Count);
        Assert.Equal("approved", detail.ApprovalTrail[0].Status);
        Assert.Equal("approved", detail.ApprovalTrail[1].Status);
        Assert.Equal("pending", detail.ApprovalTrail[2].Status);
        Assert.Equal(11, detail.BalanceImpact?.Current);
        Assert.Equal(6, detail.BalanceImpact?.After);
    }

    [Fact]
    public void MapDashboard_widgets_use_flat_employee_rows()
    {
        var onLeaveRow = new LeaveRequestRawRow(
            "req-on-leave",
            EmployeeId.ToString(),
            "Ama Asante",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 9, 24),
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
            SampleAuditAt,
            SampleAuditAt,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var pendingRow = new LeaveRequestRawRow(
            "req-pending",
            EmployeeId.ToString(),
            "Kwame Asare",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 6, 19),
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

        var leavingRow = new LeaveRequestRawRow(
            "req-leaving",
            EmployeeId.ToString(),
            "Efua Sutherland",
            LeaveTypeId.ToString(),
            "Maternity Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 11),
            2,
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
            SampleAuditAt,
            SampleAuditAt,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var onLeave = LeaveMapper.MapDashboardOnLeaveItem(
            onLeaveRow, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);
        var pending = LeaveMapper.MapDashboardPendingItem(
            pendingRow, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);
        var leaving = LeaveMapper.MapDashboardLeavingItem(
            leavingRow, NoEmployees, NoLeaveTypes, NoUserNames, NoProfileUrls);

        Assert.Equal(SampleAuditAt, onLeave.CreatedAt);
        Assert.Equal(SampleAuditAt, pending.UpdatedAt);

        Assert.Equal("Ama Asante", onLeave.EmployeeName);
        Assert.Equal("Annual Leave", onLeave.LeaveType);
        Assert.Equal(new DateOnly(2026, 9, 25), onLeave.ReturnsOn);
        Assert.Null(onLeave.ProfileUrl);

        Assert.Equal("Kwame Asare", pending.EmployeeName);
        Assert.Equal(5, pending.LeaveDays);
        Assert.NotNull(pending.Waiting);
        Assert.Null(pending.DaysSinceLastApproval);

        Assert.Equal(new DateOnly(2026, 6, 10), leaving.StartsOn);
        Assert.Equal(2, leaving.LeaveDays);
    }

    [Fact]
    public void MapApprovalListItem_includes_employee_code_and_approver_profiles()
    {
        var submittedAt = DateTimeOffset.UtcNow.AddHours(-30);
        var row = new LeaveRequestRawRow(
            "req-approval-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 6, 19),
            5,
            LeaveRequestStatuses.Pending,
            LeaveApprovalStages.PendingFinal,
            null,
            null,
            "lm-user",
            submittedAt.AddHours(-8),
            "hod-user",
            submittedAt.AddHours(-4),
            null,
            null,
            submittedAt,
            null,
            SampleAuditAt,
            SampleAuditAt,
            null,
            null);

        var employees = new Dictionary<Guid, EmployeeLeaveContext>
        {
            [EmployeeId] = new(
                EmployeeId,
                "Jane Doe",
                "ZEL-0099",
                "Senior Product Designer",
                null,
                null,
                null,
                null,
                null),
        };

        var approverUsers = new Dictionary<string, CpUserDto>
        {
            ["lm-user"] = new("lm-user", "Fiifi Boakye", "fiifi@example.com", null, true, ProfilePic: "doc-lm"),
            ["hod-user"] = new("hod-user", "Kwame Mensah", "kwame@example.com", null, true, ProfilePic: "doc-hod"),
        };

        var approverProfiles = new Dictionary<string, DocumentReadDto?>
        {
            ["lm-user"] = new DocumentReadDto
            {
                DocId = "doc-lm",
                Name = "fiifi.jpg",
                PresignedUrl = "https://example.com/fiifi.jpg",
            },
            ["hod-user"] = null,
        };

        var profileUrls = new Dictionary<Guid, DocumentReadDto?>();

        var item = LeaveMapper.MapApprovalListItem(
            row,
            employees,
            new Dictionary<Guid, string> { [LeaveTypeId] = "Annual Leave" },
            NoUserNames,
            approverUsers,
            approverProfiles,
            profileUrls);

        Assert.Equal("ZEL-0099", item.EmployeeCode);
        Assert.Equal("Senior Product Designer", item.Title);
        Assert.Equal(2, item.ApprovedBy.Count);
        Assert.Equal("Fiifi Boakye", item.ApprovedBy[0].Name);
        Assert.NotNull(item.ApprovedBy[0].ProfileUrl);
        Assert.Equal("Kwame Mensah", item.ApprovedBy[1].Name);
        Assert.Null(item.ApprovedBy[1].ProfileUrl);
        Assert.NotNull(item.Waiting);
    }

    [Fact]
    public void ExpandCalendarItems_emits_flat_rows_per_leave_range()
    {
        var approvedRow = new LeaveRequestRawRow(
            "req-cal-1",
            EmployeeId.ToString(),
            "Jane Doe",
            LeaveTypeId.ToString(),
            "Annual Leave",
            new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 14),
            8,
            LeaveRequestStatuses.Approved,
            LeaveApprovalStages.Approved,
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
            SampleAuditAt,
            null,
            null);

        var pendingRow = new LeaveRequestRawRow(
            "req-cal-2",
            EmployeeId.ToString(),
            "Jane Doe",
            MaternityLeaveTypeId.ToString(),
            "Maternity Leave",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 12),
            3,
            LeaveRequestStatuses.Pending,
            LeaveApprovalStages.PendingFinal,
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
            SampleAuditAt,
            null,
            null);

        var employees = new Dictionary<Guid, EmployeeLeaveContext>
        {
            [EmployeeId] = new(
                EmployeeId,
                "Jane Doe",
                "ZEL-0042",
                "Senior Product Designer",
                null,
                null,
                null,
                null,
                null),
        };

        var profileUrls = new Dictionary<Guid, DocumentReadDto?>
        {
            [EmployeeId] = new()
            {
                DocId = "doc-profile",
                Name = "jane.jpg",
                PresignedUrl = "https://example.com/jane.jpg",
            },
        };

        var leaveTypes = new Dictionary<Guid, string>
        {
            [LeaveTypeId] = "Annual Leave",
            [MaternityLeaveTypeId] = "Maternity Leave",
        };

        var rows = LeaveMapper.ExpandCalendarItems(
            EmployeeId,
            employees,
            profileUrls,
            [approvedRow, pendingRow],
            leaveTypes);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Jane Doe", rows[0].EmployeeName);
        Assert.Equal("ZEL-0042", rows[0].EmployeeCode);
        Assert.Equal("Approved", rows[0].Status);
        Assert.Equal(new DateOnly(2026, 6, 5), rows[0].LeaveFrom);
        Assert.Equal("Pending", rows[1].Status);
        Assert.Equal("Maternity Leave", rows[1].LeaveType);

        var emptyRows = LeaveMapper.ExpandCalendarItems(
            EmployeeId,
            employees,
            profileUrls,
            [],
            leaveTypes);

        Assert.Single(emptyRows);
        Assert.Null(emptyRows[0].LeaveRequestId);
        Assert.Null(emptyRows[0].LeaveFrom);
    }

    [Fact]
    public void LeaveCalendarWindow_week_view_uses_monday_to_sunday()
    {
        var window = LeaveCalendarWindow.Resolve(
            LeaveCalendarViews.Week,
            new DateOnly(2026, 6, 5),
            null,
            null);

        Assert.Equal(LeaveCalendarViews.Week, window.View);
        Assert.Equal(new DateOnly(2026, 6, 1), window.FromDate);
        Assert.Equal(new DateOnly(2026, 6, 7), window.ToDate);
    }

    [Theory]
    [InlineData(true, 2026, 3, 6, 2026, 2026, 3, 6)]
    [InlineData(true, 2027, 12, 25, 2027, 2027, 12, 25)]
    [InlineData(false, 2026, 3, 6, 2026, 2026, 3, 6)]
    public void ProjectHolidayOccurrence_maps_recurring_and_one_off(
        bool recurring, int anchorYear, int month, int day, int listYear, int expectedYear, int expectedMonth, int expectedDay)
    {
        var anchor = new DateOnly(anchorYear, month, day);
        var occurrence = LeaveMapper.ProjectHolidayOccurrence(anchor, recurring, listYear);

        if (!recurring && anchorYear != listYear)
        {
            Assert.Null(occurrence);
            return;
        }

        Assert.NotNull(occurrence);
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), occurrence);
    }
}
