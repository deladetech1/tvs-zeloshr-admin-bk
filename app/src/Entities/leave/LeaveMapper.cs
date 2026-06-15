using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Entities.Leave;

public sealed record LeaveRequestRawRow(
    string LeaveRequestId,
    string EmployeeId,
    string EmployeeFullName,
    string LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysRequested,
    string Status,
    string ApprovalStage,
    string? ApproverId,
    string? ApproverName,
    string? LmApproverId,
    DateTimeOffset? LmDecidedAt,
    string? HodApproverId,
    DateTimeOffset? HodDecidedAt,
    string? Notes,
    decimal? RemainingDays,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? DecidedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);

public sealed record LeaveBalanceRawRow(
    string LeaveBalanceId,
    string EmployeeId,
    string EmployeeFullName,
    string LeaveTypeId,
    string LeaveTypeName,
    decimal EntitledDays,
    decimal UsedDays,
    decimal RemainingDays,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);

internal static class LeaveMapper
{
    internal static IEnumerable<string> CollectUserIds(LeaveRequestRawRow row)
    {
        foreach (var id in CollectApproverIds(row))
            yield return id;
        if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            yield return row.CreatedBy;
        if (!string.IsNullOrWhiteSpace(row.UpdatedBy))
            yield return row.UpdatedBy;
    }

    internal static IEnumerable<string> CollectUserIds(IEnumerable<LeaveRequestRawRow> rows) =>
        rows.SelectMany(CollectUserIds);

    internal static IEnumerable<string> CollectUserIds(LeaveBalanceRawRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            yield return row.CreatedBy;
        if (!string.IsNullOrWhiteSpace(row.UpdatedBy))
            yield return row.UpdatedBy;
    }

    internal static IEnumerable<string> CollectUserIds(IEnumerable<LeaveBalanceRawRow> rows) =>
        rows.SelectMany(CollectUserIds);

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
            if (!string.IsNullOrWhiteSpace(item.Approver?.ApproverId))
                yield return item.Approver.ApproverId;

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
        IReadOnlyDictionary<Guid, string> leaveTypes,
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

        var leaveType = ResolveLeaveType(row.LeaveTypeId, row.LeaveTypeName, leaveTypes);

        var finalApprover = ResolveApprover(row.ApproverId, approverNames, row.ApproverName);
        var audit = LeaveAuditFields.Map(
            row.CreatedAt,
            row.UpdatedAt,
            row.CreatedBy,
            row.UpdatedBy,
            approverNames);
        var item = new LeaveRequestListItemDto
        {
            LeaveRequestId = row.LeaveRequestId,
            Employee = employee is null
                ? ToEmployeeFallback(row.EmployeeId, row.EmployeeFullName)
                : ToEmployeeRef(row.EmployeeId, employee, profileUrl),
            LeaveType = leaveType,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            DaysRequested = row.DaysRequested,
            Status = row.Status,
            ApprovalStage = row.ApprovalStage,
            Approver = finalApprover,
            PriorApprovers = BuildPriorApprovers(row, approverNames),
            Notes = row.Notes,
            RemainingDays = row.RemainingDays,
            WaitingHours = ComputeWaitingHours(row),
            ReturnsOn = row.EndDate.AddDays(1),
            DaysSinceLastApproval = ComputeDaysSinceLastApproval(row),
            SubmittedAt = row.SubmittedAt,
            DecidedAt = row.DecidedAt,
            CreatedAt = audit.CreatedAt,
            UpdatedAt = audit.UpdatedAt,
            CreatedById = audit.CreatedById,
            UpdatedById = audit.UpdatedById,
            CreatedBy = audit.CreatedBy,
            UpdatedBy = audit.UpdatedBy,
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
        var finalApprover = ResolveApprover(row.ApproverId, approverNames, row.ApproverName);
        return new LeaveRequestDetailDto
        {
            LeaveRequestId = item.LeaveRequestId,
            Employee = item.Employee,
            LeaveType = item.LeaveType,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            DaysRequested = item.DaysRequested,
            WorkingDays = workingDays,
            PublicHolidaysInRange = publicHolidaysInRange,
            Status = item.Status,
            ApprovalStage = item.ApprovalStage,
            Approver = finalApprover,
            ApprovalTrail = BuildApprovalTrail(row, finalApprover, approverNames),
            Notes = item.Notes,
            RemainingDays = item.RemainingDays,
            BalanceImpact = balanceImpact,
            WaitingHours = item.WaitingHours,
            SubmittedAt = item.SubmittedAt,
            DecidedAt = item.DecidedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            CreatedById = item.CreatedById,
            UpdatedById = item.UpdatedById,
            CreatedBy = item.CreatedBy,
            UpdatedBy = item.UpdatedBy,
        };
    }

    internal static LeaveBalanceListItemDto MapBalance(
        LeaveBalanceRawRow row,
        IReadOnlyDictionary<Guid, EmployeeLeaveContext> employees,
        IReadOnlyDictionary<Guid, string> leaveTypes,
        IReadOnlyDictionary<string, string> userNames,
        IReadOnlyDictionary<Guid, DocumentReadDto?> profileUrlsByEmployeeId)
    {
        EmployeeLeaveContext? employee = null;
        DocumentReadDto? profileUrl = null;
        if (Guid.TryParse(row.EmployeeId, out var employeeId))
        {
            employees.TryGetValue(employeeId, out employee);
            profileUrlsByEmployeeId.TryGetValue(employeeId, out profileUrl);
        }

        var audit = LeaveAuditFields.Map(
            row.CreatedAt,
            row.UpdatedAt,
            row.CreatedBy,
            row.UpdatedBy,
            userNames);

        return new LeaveBalanceListItemDto
        {
            LeaveBalanceId = row.LeaveBalanceId,
            Employee = employee is null
                ? ToEmployeeFallback(row.EmployeeId, row.EmployeeFullName)
                : ToEmployeeRef(row.EmployeeId, employee, profileUrl),
            LeaveType = ResolveLeaveType(row.LeaveTypeId, row.LeaveTypeName, leaveTypes),
            EntitledDays = row.EntitledDays,
            UsedDays = row.UsedDays,
            RemainingDays = row.RemainingDays,
            CreatedAt = audit.CreatedAt,
            UpdatedAt = audit.UpdatedAt,
            CreatedById = audit.CreatedById,
            UpdatedById = audit.UpdatedById,
            CreatedBy = audit.CreatedBy,
            UpdatedBy = audit.UpdatedBy,
        };
    }

    internal static IReadOnlyDictionary<Guid, string> IndexTypes(
        IEnumerable<LeaveTypeListItemDto> types) =>
        types
            .Where(t => Guid.TryParse(t.LeaveTypeId, out _))
            .ToDictionary(t => Guid.Parse(t.LeaveTypeId), t => t.Name);

    internal static Dictionary<Guid, string> MergeLeaveTypeLookups(
        IReadOnlyDictionary<Guid, string> indexedTypes,
        IEnumerable<(string LeaveTypeId, string LeaveTypeName)> rows)
    {
        var leaveTypes = new Dictionary<Guid, string>(indexedTypes);
        foreach (var (leaveTypeId, leaveTypeName) in rows)
        {
            if (string.IsNullOrWhiteSpace(leaveTypeName))
                continue;
            if (Guid.TryParse(leaveTypeId, out var typeId) && !leaveTypes.ContainsKey(typeId))
                leaveTypes[typeId] = leaveTypeName;
        }

        return leaveTypes;
    }

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
        return users
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.FullName))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FullName);
    }

    internal static LeaveTypeListItemDto EnrichTypeAudit(
        LeaveTypeListItemDto item,
        IReadOnlyDictionary<string, string> userNames)
    {
        var audit = LeaveAuditFields.Map(
            item.CreatedAt, item.UpdatedAt, item.CreatedById, item.UpdatedById, userNames);
        return new LeaveTypeListItemDto
        {
            LeaveTypeId = item.LeaveTypeId,
            Name = item.Name,
            DefaultEntitledDays = item.DefaultEntitledDays,
            IsPaid = item.IsPaid,
            AccrualMethod = item.AccrualMethod,
            CarryOverAllowed = item.CarryOverAllowed,
            AppliesToEmploymentTypes = item.AppliesToEmploymentTypes,
            MinNoticeWorkingDays = item.MinNoticeWorkingDays,
            MaxConsecutiveDays = item.MaxConsecutiveDays,
            RequiresSupportingDocument = item.RequiresSupportingDocument,
            CreatedAt = audit.CreatedAt,
            UpdatedAt = audit.UpdatedAt,
            CreatedById = audit.CreatedById,
            UpdatedById = audit.UpdatedById,
            CreatedBy = audit.CreatedBy,
            UpdatedBy = audit.UpdatedBy,
        };
    }

    internal static PublicHolidayListItemDto EnrichHolidayAudit(
        PublicHolidayListItemDto item,
        IReadOnlyDictionary<string, string> userNames)
    {
        var audit = LeaveAuditFields.Map(
            item.CreatedAt, item.UpdatedAt, item.CreatedById, item.UpdatedById, userNames);
        return new PublicHolidayListItemDto
        {
            HolidayId = item.HolidayId,
            CountryCode = item.CountryCode,
            Name = item.Name,
            HolidayDate = item.HolidayDate,
            IsRecurring = item.IsRecurring,
            BranchId = item.BranchId,
            IsActive = item.IsActive,
            CreatedAt = audit.CreatedAt,
            UpdatedAt = audit.UpdatedAt,
            CreatedById = audit.CreatedById,
            UpdatedById = audit.UpdatedById,
            CreatedBy = audit.CreatedBy,
            UpdatedBy = audit.UpdatedBy,
        };
    }

    private static LeaveEmployeeRefDto ToEmployeeRef(
        string employeeId,
        EmployeeLeaveContext employee,
        DocumentReadDto? profileUrl) =>
        new()
        {
            EmployeeId = employeeId,
            FullName = employee.FullName,
            EmployeeCode = employee.EmployeeCode,
            JobTitle = employee.JobTitle,
            DepartmentId = employee.DepartmentId?.ToString(),
            DepartmentName = employee.DepartmentName,
            ProfileUrl = profileUrl,
        };

    private static LeaveEmployeeRefDto? ToEmployeeFallback(string employeeId, string employeeFullName)
    {
        if (string.IsNullOrWhiteSpace(employeeFullName))
            return null;

        return new LeaveEmployeeRefDto
        {
            EmployeeId = employeeId,
            FullName = employeeFullName,
        };
    }

    private static LeaveTypeRefDto? ResolveLeaveType(
        string leaveTypeId,
        string leaveTypeName,
        IReadOnlyDictionary<Guid, string> leaveTypes)
    {
        if (Guid.TryParse(leaveTypeId, out var id) && leaveTypes.TryGetValue(id, out var name))
            return new LeaveTypeRefDto { LeaveTypeId = leaveTypeId, Name = name };
        if (!string.IsNullOrWhiteSpace(leaveTypeName))
            return new LeaveTypeRefDto { LeaveTypeId = leaveTypeId, Name = leaveTypeName };
        return null;
    }

    private static LeaveApproverRefDto? ResolveApprover(
        string? approverId,
        IReadOnlyDictionary<string, string> approverNames,
        string? fallbackName = null)
    {
        if (string.IsNullOrWhiteSpace(approverId))
            return null;

        approverNames.TryGetValue(approverId, out var name);
        var displayName = !string.IsNullOrWhiteSpace(name)
            ? name
            : fallbackName;
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        return new LeaveApproverRefDto
        {
            ApproverId = approverId,
            FullName = displayName,
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

        if (ComputeDaysSinceLastApproval(row).HasValue)
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

    private static int? ComputeDaysSinceLastApproval(LeaveRequestRawRow row)
    {
        if (!row.Status.Equals(LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return null;

        var last = row.HodDecidedAt ?? row.LmDecidedAt;
        if (!last.HasValue)
            return null;

        var elapsed = DateTimeOffset.UtcNow - last.Value;
        return elapsed.TotalDays < 1 ? 1 : (int)Math.Floor(elapsed.TotalDays);
    }
}
