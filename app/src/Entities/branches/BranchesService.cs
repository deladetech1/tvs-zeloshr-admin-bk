using Dapper;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Branches;

public class BranchesService
{
    private readonly IDatabaseManager _database;
    private readonly AppSettings _settings;

    public BranchesService(IDatabaseManager database, IOptions<AppSettings> settings)
    {
        _database = database;
        _settings = settings.Value;
    }

    public async Task<Respons<BranchListDto>> ListBranchesAsync(
        string tenantId,
        string orgId,
        bool includeArchived = false,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var rows = await connection.QueryAsync<BranchRow>(
            $"""
            SELECT
                b.id AS Id,
                b.name AS Name,
                b.is_archived AS IsArchived,
                (
                    SELECT COUNT(*)::int FROM {_settings.EmployeesTable} e
                    WHERE e.branch_id = b.id AND e.is_deleted = FALSE
                ) AS EmployeeCount
            FROM zeloshr.zhr_branches b
            WHERE b.tenant_id = @TenantId AND b.org_id = @OrgId
              AND (@IncludeArchived OR b.is_archived = FALSE)
            ORDER BY b.name
            """,
            new { TenantId = tenantId, OrgId = orgId, IncludeArchived = includeArchived });

        var items = rows.Select(r => new BranchListItemDto
        {
            BranchId = r.Id.ToString(),
            Name = r.Name,
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
        }).ToList();

        return Respons<BranchListDto>.Ok(new BranchListDto { Items = items });
    }

    private sealed class BranchRow
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public int EmployeeCount { get; init; }
        public bool IsArchived { get; init; }
    }
}
