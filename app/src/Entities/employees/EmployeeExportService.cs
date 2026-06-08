using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeExportService
{
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly ITenantContext _tenant;

    public EmployeeExportService(
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        ITenantContext tenant)
    {
        _employees = employees;
        _cpUsers = cpUsers;
        _tenant = tenant;
    }

    public async Task<Respons<byte[]>> ExportCsvAsync(EmployeeExportQuery query, CancellationToken ct = default)
    {
        if (query.StartDate is not null && query.EndDate is not null && query.StartDate > query.EndDate)
        {
            return Respons<byte[]>.ValidationError(new Dictionary<string, string>
            {
                ["start_date"] = "start_date must be on or before end_date.",
            });
        }

        var rows = await _employees.ExportListScopedAsync(
            query, _tenant.TenantId, _tenant.OrgId, ct);

        var userIds = rows
            .Select(r => r.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct();
        var platformUsers = await _cpUsers.GetByIdsAsync(userIds, _tenant.TenantId, ct);

        var csv = EmployeeCsvExport.Build(rows, platformUsers);
        return Respons<byte[]>.Ok(csv);
    }
}
