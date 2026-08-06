using ZelosHR.Api.Entities.Employees;
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

    public async Task<(bool Success, EmployeeIdFormatEntity? Format, Dictionary<string, string>? Errors)>
        TryGetFormatAsync(string tenantId, string orgId, CancellationToken ct = default)
    {
        var format = await _formats.GetEntityAsync(tenantId, orgId, ct);
        if (format is not null)
            return (true, format, null);

        return (false, null, new Dictionary<string, string>
        {
            ["employee_id_format"] =
                "Employee ID format is not configured for this organisation. Create settings via POST /company/id-format/add.",
        });
    }

    public async Task<string> PreviewNextCodeAsync(
        EmployeeIdFormatEntity format,
        string tenantId,
        CancellationToken ct = default)
    {
        var next = await ComputeNextSequenceAsync(tenantId, format, ct);
        return EmployeeIdFormatRules.Format(format, next);
    }

    public async Task<long> ComputeNextSequenceAsync(
        string tenantId,
        EmployeeIdFormatEntity format,
        CancellationToken ct = default)
    {
        var codes = await _employees.ListEmployeeSystemCodesAsync(tenantId, ct);
        return EmployeeIdFormatRules.ComputeNextSequence(codes, format);
    }

    public string FormatCode(EmployeeIdFormatEntity format, long sequence) =>
        EmployeeIdFormatRules.Format(format, sequence);

    public sealed record EmployeeCodeCreatePlan(
        EmployeeIdFormatEntity Format,
        long StartSequence,
        string InitialSystemCode,
        string? CustomCode);

    public async Task<(bool Success, EmployeeCodeCreatePlan? Plan, Dictionary<string, string>? Errors)>
        PlanCreateAsync(
            string tenantId,
            string orgId,
            string? requestedCustomCode,
            CancellationToken ct = default)
    {
        var formatResult = await TryGetFormatAsync(tenantId, orgId, ct);
        if (!formatResult.Success)
            return (false, null, formatResult.Errors);

        var format = formatResult.Format!;
        var customCode = EmployeeCodeResolver.NormalizeCustom(requestedCustomCode);

        if (customCode is null && !format.AutoGenerate)
        {
            return (false, null, new Dictionary<string, string>
            {
                ["employee_code_custom"] =
                    "employee_code_custom is required. Auto-generate is disabled for this organisation.",
            });
        }

        if (customCode is not null && customCode.Length > EmployeeIdFormatRules.MaxEmployeeCodeLength)
        {
            return (false, null, new Dictionary<string, string>
            {
                ["employee_code_custom"] =
                    $"employee_code_custom must be at most {EmployeeIdFormatRules.MaxEmployeeCodeLength} characters.",
            });
        }

        var startSeq = await ComputeNextSequenceAsync(tenantId, format, ct);
        var systemCode = FormatCode(format, startSeq);
        return (true, new EmployeeCodeCreatePlan(format, startSeq, systemCode, customCode), null);
    }

    public void ApplyToEntity(EmployeeEntity entity, EmployeeCodeCreatePlan plan, int attempt)
    {
        entity.EmployeeCodeSystem = FormatCode(plan.Format, plan.StartSequence + attempt);
        entity.EmployeeCodeCustom = plan.CustomCode;
    }

    public string ResolveSystemCode(EmployeeCodeCreatePlan plan, int attempt) =>
        FormatCode(plan.Format, plan.StartSequence + attempt);
}
