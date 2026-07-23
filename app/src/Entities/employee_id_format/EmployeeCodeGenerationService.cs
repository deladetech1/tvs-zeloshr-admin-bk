using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.EmployeeIdFormat;

/// <summary>Resolves org employee ID format settings and allocates codes on create.</summary>
public sealed class EmployeeCodeGenerationService
{
    private readonly IEmployeeIdFormatRepository _formats;
    private readonly IEmployeeRepository _employees;

    public EmployeeCodeGenerationService(
        IEmployeeIdFormatRepository formats,
        IEmployeeRepository employees)
    {
        _formats = formats;
        _employees = employees;
    }

    public async Task<EmployeeIdFormatEntity> GetFormatAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default) =>
        await _formats.EnsureStubAsync(tenantId, orgId, actorUserId, ct);

    public async Task<string> PreviewNextCodeAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var format = await GetFormatAsync(tenantId, orgId, actorUserId, ct);
        var next = await ComputeNextSequenceAsync(tenantId, format, ct);
        return EmployeeIdFormatRules.Format(format, next);
    }

    public async Task<long> ComputeNextSequenceAsync(
        string tenantId,
        EmployeeIdFormatEntity format,
        CancellationToken ct = default)
    {
        var codes = await _employees.ListEmployeeCodesAsync(tenantId, ct);
        return EmployeeIdFormatRules.ComputeNextSequence(codes, format);
    }

    public string FormatCode(EmployeeIdFormatEntity format, long sequence) =>
        EmployeeIdFormatRules.Format(format, sequence);

    public async Task<(bool Success, string? Code, Dictionary<string, string>? Errors)> ResolveForCreateAsync(
        string tenantId,
        string orgId,
        string? requestedCode,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var format = await GetFormatAsync(tenantId, orgId, actorUserId, ct);
        if (format.AutoGenerate)
        {
            var next = await ComputeNextSequenceAsync(tenantId, format, ct);
            return (true, EmployeeIdFormatRules.Format(format, next), null);
        }

        if (string.IsNullOrWhiteSpace(requestedCode))
        {
            return (false, null, new Dictionary<string, string>
            {
                ["employee_code"] =
                    "Employee ID is required. Auto-generate is disabled for this organisation — provide employee_code.",
            });
        }

        var trimmed = requestedCode.Trim();
        if (trimmed.Length > EmployeeIdFormatRules.MaxEmployeeCodeLength)
        {
            return (false, null, new Dictionary<string, string>
            {
                ["employee_code"] =
                    $"Employee ID must be at most {EmployeeIdFormatRules.MaxEmployeeCodeLength} characters.",
            });
        }

        return (true, trimmed, null);
    }

    internal sealed record CodeAllocationPlan(
        EmployeeIdFormatEntity Format,
        long StartSequence,
        string InitialCode,
        bool UseRetry);

    public async Task<(bool Success, CodeAllocationPlan? Plan, Dictionary<string, string>? Errors)>
        PlanAllocationAsync(
            string tenantId,
            string orgId,
            string? requestedCode,
            string? actorUserId,
            CancellationToken ct = default)
    {
        var resolved = await ResolveForCreateAsync(tenantId, orgId, requestedCode, actorUserId, ct);
        if (!resolved.Success)
            return (false, null, resolved.Errors);

        var format = await GetFormatAsync(tenantId, orgId, actorUserId, ct);
        if (!format.AutoGenerate)
        {
            return (true, new CodeAllocationPlan(format, 0, resolved.Code!, UseRetry: false), null);
        }

        var startSeq = await ComputeNextSequenceAsync(tenantId, format, ct);
        return (true, new CodeAllocationPlan(format, startSeq, FormatCode(format, startSeq), UseRetry: true), null);
    }
}
