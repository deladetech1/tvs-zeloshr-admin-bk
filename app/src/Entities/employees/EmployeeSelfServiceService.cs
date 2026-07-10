using System.Text.Json;
using System.Text.Json.Nodes;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees.Authorization;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeSelfServiceService
{
    private readonly IEmployeeLookup _employeeLookup;
    private readonly IEmployeeUpdateService _employeeUpdate;
    private readonly ChangeRequestService _changeRequests;
    private readonly ITenantContextAccessor _tenant;

    public EmployeeSelfServiceService(
        IEmployeeLookup employeeLookup,
        IEmployeeUpdateService employeeUpdate,
        ChangeRequestService changeRequests,
        ITenantContextAccessor tenant)
    {
        _employeeLookup = employeeLookup;
        _employeeUpdate = employeeUpdate;
        _changeRequests = changeRequests;
        _tenant = tenant;
    }

    public async Task<(Guid EmployeeId, Respons<EmployeeSelfUpdateResultDto>? Error)> ResolveLinkedEmployeeAsync(
        CancellationToken ct = default)
    {
        var tenant = _tenant.Current;
        var platformUserId = tenant.UserId;
        if (string.IsNullOrWhiteSpace(platformUserId))
            return (Guid.Empty, Respons<EmployeeSelfUpdateResultDto>.Forbidden("Authenticated user is required."));

        var resolved = await _employeeLookup.ResolveByPlatformUserAsync(
            platformUserId, tenant.TenantId, tenant.OrgId, ct);
        if (resolved is null)
            return (Guid.Empty, Respons<EmployeeSelfUpdateResultDto>.NotFound(
                "No employee profile is linked to the current user."));

        return (resolved.Value.EmployeeId, null);
    }

    public async Task<Respons<EmployeeSelfUpdateResultDto>> SelfUpdateAsync(
        JsonObject payload,
        CancellationToken ct = default)
    {
        var (employeeId, resolveError) = await ResolveLinkedEmployeeAsync(ct);
        if (resolveError is not null)
            return resolveError;

        var tenant = _tenant.Current;
        var platformUserId = tenant.UserId!;

        var current = await _employeeUpdate.GetAsync(employeeId, ct);
        if (!current.Success || current.Data is null)
        {
            return new Respons<EmployeeSelfUpdateResultDto>
            {
                Detail = current.Detail,
                Success = false,
                StatusCode = current.StatusCode,
                Error = current.Error,
            };
        }

        var updateShape = EmployeeAggregateJsonSnapshot.ToUpdateShape(current.Data);
        var split = EmployeePayloadFilter.Split(
            payload,
            path => EmployeeAggregateJsonSnapshot.ResolvePath(updateShape, path));

        Respons<EmployeeAggregateReadDto>? applyResult = null;
        var appliedPaths = split.ApplyNow.Count > 0
            ? CollectAppliedPaths(split.ApplyNow)
            : [];

        if (split.ApplyNow.Count > 0)
        {
            var request = JsonSerializer.Deserialize<UpdateEmployeeAggregateRequest>(
                split.ApplyNow.ToJsonString(),
                PlatformJson.SerializerOptions);

            if (request is null)
            {
                return Respons<EmployeeSelfUpdateResultDto>.ValidationError(new Dictionary<string, string>
                {
                    ["body"] = "Could not deserialize apply-now payload.",
                });
            }

            applyResult = await _employeeUpdate.ApplyAsync(employeeId, request, ct);
            if (!applyResult.Success)
            {
                return new Respons<EmployeeSelfUpdateResultDto>
                {
                    Detail = applyResult.Detail,
                    Success = false,
                    StatusCode = applyResult.StatusCode,
                    Error = applyResult.Error,
                    FieldErrors = applyResult.FieldErrors,
                };
            }
        }

        var pending = await _changeRequests.CreatePendingAsync(employeeId, platformUserId, split.PendingApproval, ct);

        if (applyResult?.Data is null && pending.Count == 0 && split.Rejected.Count == 0)
        {
            return Respons<EmployeeSelfUpdateResultDto>.ValidationError(new Dictionary<string, string>
            {
                ["body"] = "Include at least one field to update.",
            });
        }

        var employee = applyResult?.Data ?? current.Data;

        return Respons<EmployeeSelfUpdateResultDto>.Ok(new EmployeeSelfUpdateResultDto
        {
            Employee = employee,
            Pending = pending,
            Applied = appliedPaths,
            Rejected = split.Rejected,
        });
    }

    private static IReadOnlyList<string> CollectAppliedPaths(JsonObject applyNow)
    {
        var paths = new List<string>();
        foreach (var (key, node) in applyNow)
        {
            if (node is JsonObject nested)
            {
                foreach (var (prop, _) in nested)
                    paths.Add($"{key}.{prop}");
            }
            else
            {
                paths.Add(key);
            }
        }

        return paths;
    }
}
