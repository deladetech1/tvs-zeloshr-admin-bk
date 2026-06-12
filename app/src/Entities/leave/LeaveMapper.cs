using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Entities.Leave;

public sealed record LeaveRequestRawRow(
    string LeaveRequestId,
    string EmployeeId,
    string LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysRequested,
    string Status,
    string ApprovalStage,
    string? ApproverId,
    string? LmApproverId,
    DateTimeOffset? LmDecidedAt,
    string? HodApproverId,
    DateTimeOffset? HodDecidedAt,
    string? Notes,
    decimal? RemainingDays,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? DecidedAt);

internal static class LeaveMapper
{
    internal static IEnumerable<string> CollectApproverIds(IEnumerable<LeaveRequestRawRow> rows) =>
        rows.SelectMany(r => CollectApproverIds(r));

    internal static IEnumerable<string> CollectApproverIds(LeaveRequestRawRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.ApproverId))
            yield return row.ApproverId;
        if (!string.IsNullOrWhiteSpace(row.LmApproverId))
            yield return row.LmApproverId;
        if (!string.IsNullOrWhiteSpace(row.HodApproverId))
            yield return row.HodApproverId;
    }

    internal static IEnumerable<string> CollectApproverIds(IEnumerable<LeaveRequestListItemDto> items)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.ApproverId))
                yield return item.ApproverId;

            foreach (var step in item.PriorApprovers)
            {
                if (!string.IsNullOrWhiteSpace(step.Approver?.ApproverId))
                    yield return step.Approver.ApproverId;
            }
        }
    }

    internal static LeaveRequestListItemDto MapRequest(
        LeaveRequestRawRow row,
        IReadOnlyDictionary<Guid, EmployeeLeaveContext> employees,
        IReadOnlyDictionary<Guid, LeaveTypeRefDto> leaveTypes,
        IReadOnlyDictionary<string, string> approverNames,
        IReadOnlyDictionary<Guid, DocumentReadDto?> profileUrlsByEmployeeId)
    {
        EmployeeLeaveContext? employee = null;
        DocumentReadDto? profileUrl = null;
        if (Guid.TryParse(row.EmployeeId, out var employeeId))
        {
            employees.TryGetValue(employeeId, out employee);
            profileUrlsByEmployeeId.TryGetValue(employeeId, out profileUrl);
        }

        LeaveTypeRefDto? leaveType = null;
        if (Guid.TryParse(row.LeaveTypeId, out var leaveTypeId))
            leaveTypes.TryGetValue(leaveTypeId, out leaveType);
        else if (!string.IsNullOrWhiteSpace(row.LeaveTypeName))
            leaveType = new LeaveTypeRefDto { LeaveTypeId = row.LeaveTypeId, Name = row.LeaveTypeName };

        var finalApprover = ResolveApprover(row.ApproverId, approverNames);
        var item = new LeaveRequestListItemDto
        {
            LeaveRequestId = row.LeaveRequestId,
            EmployeeId = row.EmployeeId,
            LeaveTypeId = row.LeaveTypeId,
            Employee = employee is null ? null : ToEmployeeRef(employee, profileUrl),
            LeaveType = leaveType,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            DaysRequested = row.DaysRequested,
            Status = row.Status,
            ApprovalStage = row.ApprovalStage,
            ApproverId = row.ApproverId,
            Approver = finalApprover,
            PriorApprovers = BuildPriorApprovers(row, approverNames),
            Notes = row.Notes,
            RemainingDays = row.RemainingDays,
            WaitingHours = ComputeWaitingHours(row),
            SubmittedAt = row.SubmittedAt,
            DecidedAt = row.DecidedAt,
        };

        return item;
    }

    internal static LeaveRequestDetailDto ToDetail(
        LeaveRequestListItemDto item,
        LeaveRequestRawRow row,
        decimal workingDays,
        int publicHolidaysInRange,
        LeaveBalanceImpactDto? balanceImpact,
        IReadOnlyDictionary<string, string> approverNames)
    {
        var finalApprover = ResolveApprover(row.ApproverId, approverNames);
        return new LeaveRequestDetailDto
        {
            LeaveRequestId = item.LeaveRequestId,
            EmployeeId = item.EmployeeId,
            LeaveTypeId = item.LeaveTypeId,
            Employee = item.Employee,
            LeaveType = item.LeaveType,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            DaysRequested = item.DaysRequested,
            WorkingDays = workingDays,
            PublicHolidaysInRange = publicHolidaysInRange,
            Status = item.Status,
            ApprovalStage = item.ApprovalStage,
            ApproverId = item.ApproverId,
            Approver = finalApprover,
            ApprovalTrail = BuildApprovalTrail(row, finalApprover, approverNames),
            Notes = item.Notes,
            RemainingDays = item.RemainingDays,
            BalanceImpact = balanceImpact,
            WaitingHours = item.WaitingHours,
            SubmittedAt = item.SubmittedAt,
            DecidedAt = item.DecidedAt,
        };
    }

    internal static LeaveBalanceListItemDto EnrichBalance(
        LeaveBalanceListItemDto item,
        IReadOnlyDictionary<Guid, LeaveTypeRefDto> leaveTypes)
    {
        if (Guid.TryParse(item.LeaveTypeId, out var leaveTypeId)
            && leaveTypes.TryGetValue(leaveTypeId, out var leaveType))
        {
            return new LeaveBalanceListItemDto
            {
                LeaveBalanceId = item.LeaveBalanceId,
                EmployeeId = item.EmployeeId,
                LeaveTypeId = item.LeaveTypeId,
                LeaveType = leaveType,
                EntitledDays = item.EntitledDays,
                UsedDays = item.UsedDays,
                RemainingDays = item.RemainingDays,
            };
        }

        return item;
    }

    internal static LeaveTypeRefDto ToTypeRef(Guid id, string name) =>
        new() { LeaveTypeId = id.ToString(), Name = name };

    internal static IReadOnlyDictionary<Guid, LeaveTypeRefDto> IndexTypes(
        IEnumerable<LeaveTypeListItemDto> types) =>
        types
            .Where(t => Guid.TryParse(t.LeaveTypeId, out _))
            .ToDictionary(t => Guid.Parse(t.LeaveTypeId), t => ToTypeRef(Guid.Parse(t.LeaveTypeId), t.Name));

    internal static LeaveBalanceImpactDto? BuildBalanceImpact(decimal? remaining, decimal daysRequested)
    {
        if (!remaining.HasValue)
            return null;

        return new LeaveBalanceImpactDto
        {
            Current = remaining.Value,
            Deduction = daysRequested,
            After = remaining.Value - daysRequested,
        };
    }

    internal static async Task<IReadOnlyDictionary<string, string>> ResolveApproverNamesAsync(
        ICpUserRepository cpUsers,
        IEnumerable<string> approverIds,
        string tenantId,
        CancellationToken ct)
    {
        var ids = approverIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<string, string>();

        var users = await cpUsers.GetByIdsAsync(ids, tenantId, ct);
        return users.ToDictionary(
            kvp => kvp.Key,
            kvp => string.IsNullOrWhiteSpace(kvp.Value.FullName) ? kvp.Key : kvp.Value.FullName);
    }

    private static LeaveEmployeeRefDto ToEmployeeRef(
        EmployeeLeaveContext employee,
        DocumentReadDto? profileUrl) =>
        new()
        {
            EmployeeId = employee.EmployeeId.ToString(),
            FullName = employee.FullName,
            EmployeeCode = employee.EmployeeCode,
            JobTitle = employee.JobTitle,
            DepartmentId = employee.DepartmentId?.ToString(),
            DepartmentName = employee.DepartmentName,
            ProfileUrl = profileUrl,
        };

    private static LeaveApproverRefDto? ResolveApprover(
        string? approverId, IReadOnlyDictionary<string, string> approverNames)
    {
        if (string.IsNullOrWhiteSpace(approverId))
            return null;

        approverNames.TryGetValue(approverId, out var name);
        return new LeaveApproverRefDto
        {
            ApproverId = approverId,
            FullName = name ?? approverId,
        };
    }

    private static IReadOnlyList<LeaveApprovalStepDto> BuildPriorApprovers(
        LeaveRequestRawRow row,
        IReadOnlyDictionary<string, string> approverNames) =>
        BuildApprovalTrail(row, null, approverNames)
            .Where(s => s.Stage is not LeaveApprovalStepLabels.Final)
            .Where(s => s.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static IReadOnlyList<LeaveApprovalStepDto> BuildApprovalTrail(
        LeaveRequestRawRow row,
        LeaveApproverRefDto? finalApprover,
        IReadOnlyDictionary<string, string> approverNames)
    {
        return
        [
            BuildStep(
                LeaveApprovalStepLabels.LineManager,
                row.ApprovalStage,
                row.LmApproverId,
                row.LmDecidedAt,
                approverNames),
            BuildStep(
                LeaveApprovalStepLabels.HeadOfDepartment,
                row.ApprovalStage,
                row.HodApproverId,
                row.HodDecidedAt,
                approverNames),
            new LeaveApprovalStepDto
            {
                Stage = LeaveApprovalStepLabels.Final,
                Status = ResolveFinalStepStatus(row),
                Approver = finalApprover,
                DecidedAt = row.DecidedAt,
            },
        ];
    }

    private static string ResolveFinalStepStatus(LeaveRequestRawRow row)
    {
        if (row.Status.Equals(LeaveRequestStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            return "approved";
        if (row.Status.Equals(LeaveRequestStatuses.Rejected, StringComparison.OrdinalIgnoreCase)
            && row.ApprovalStage.Equals(LeaveApprovalStages.Rejected, StringComparison.OrdinalIgnoreCase))
            return "rejected";
        if (row.ApprovalStage.Equals(LeaveApprovalStages.PendingFinal, StringComparison.OrdinalIgnoreCase))
            return "pending";
        return "pending";
    }

    private static LeaveApprovalStepDto BuildStep(
        string stage,
        string approvalStage,
        string? approverId,
        DateTimeOffset? decidedAt,
        IReadOnlyDictionary<string, string> approverNames)
    {
        var status = decidedAt.HasValue
            ? "approved"
            : IsStagePending(stage, approvalStage)
                ? "pending"
                : IsStagePast(stage, approvalStage)
                    ? "approved"
                    : "pending";

        return new LeaveApprovalStepDto
        {
            Stage = stage,
            Status = status,
            Approver = ResolveApprover(approverId, approverNames),
            DecidedAt = decidedAt,
        };
    }

    private static bool IsStagePending(string stage, string approvalStage) =>
        stage switch
        {
            LeaveApprovalStepLabels.LineManager =>
                approvalStage.Equals(LeaveApprovalStages.PendingLineManager, StringComparison.OrdinalIgnoreCase),
            LeaveApprovalStepLabels.HeadOfDepartment =>
                approvalStage.Equals(LeaveApprovalStages.PendingHeadOfDepartment, StringComparison.OrdinalIgnoreCase),
            _ => approvalStage.Equals(LeaveApprovalStages.PendingFinal, StringComparison.OrdinalIgnoreCase),
        };

    private static bool IsStagePast(string stage, string approvalStage)
    {
        var order = approvalStage.ToLowerInvariant() switch
        {
            LeaveApprovalStages.PendingLineManager => 0,
            LeaveApprovalStages.PendingHeadOfDepartment => 1,
            LeaveApprovalStages.PendingFinal => 2,
            LeaveApprovalStages.Approved => 3,
            LeaveApprovalStages.Rejected => 3,
            _ => 0,
        };

        var stageOrder = stage switch
        {
            LeaveApprovalStepLabels.LineManager => 0,
            LeaveApprovalStepLabels.HeadOfDepartment => 1,
            LeaveApprovalStepLabels.Final => 2,
            _ => 0,
        };

        return order > stageOrder;
    }

    private static int? ComputeWaitingHours(LeaveRequestRawRow row)
    {
        if (!row.Status.Equals(LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return null;

        var anchor = row.SubmittedAt;
        if (row.ApprovalStage.Equals(LeaveApprovalStages.PendingHeadOfDepartment, StringComparison.OrdinalIgnoreCase)
            && row.LmDecidedAt.HasValue)
            anchor = row.LmDecidedAt.Value;
        else if (row.ApprovalStage.Equals(LeaveApprovalStages.PendingFinal, StringComparison.OrdinalIgnoreCase)
                 && row.HodDecidedAt.HasValue)
            anchor = row.HodDecidedAt.Value;

        var elapsed = DateTimeOffset.UtcNow - anchor;
        return elapsed.TotalHours < 1 ? 1 : (int)Math.Floor(elapsed.TotalHours);
    }
}
