using Dapper;
using Npgsql;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.OrgStructure;

public class OrgStructureService
{
    private readonly DepartmentsService _departments;
    private readonly BranchesService _branches;
    private readonly IDatabaseManager _database;

    public OrgStructureService(
        DepartmentsService departments,
        BranchesService branches,
        IDatabaseManager database)
    {
        _departments = departments;
        _branches = branches;
        _database = database;
    }

    public Task<Respons<OrganisationSummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct) =>
        _departments.GetSummaryAsync(tenantId, orgId, ct);

    public Task<Respons<DepartmentListDto>> ListDepartmentsAsync(
        string? search, string sortBy, string sortOrder, bool includeArchived,
        int page, int size, string tenantId, string orgId, CancellationToken ct) =>
        _departments.ListDepartmentsAsync(search, sortBy, sortOrder, includeArchived, page, size, tenantId, orgId, ct);

    public Task<Respons<BranchListDto>> ListBranchesAsync(
        string tenantId, string orgId, bool includeArchived, CancellationToken ct) =>
        _branches.ListBranchesAsync(tenantId, orgId, includeArchived, ct);

    public async Task<Respons<OrgChartDto>> GetOrgChartAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var rows = await connection.QueryAsync<DepartmentChartRow>(
            """
            SELECT
                d.id AS Id,
                d.name AS Name,
                d.parent_department_id AS ParentDepartmentId,
                d.is_archived AS IsArchived,
                h.id AS HeadId,
                h.first_name AS HeadFirstName,
                h.last_name AS HeadLastName,
                h.job_title AS HeadJobTitle,
                (
                    SELECT COUNT(*)::int FROM zeloshr.zhr_employees e
                    WHERE e.department_id = d.id AND e.is_deleted = FALSE
                ) AS EmployeeCount
            FROM zeloshr.zhr_departments d
            LEFT JOIN zeloshr.zhr_employees h ON h.id = d.head_of_department_id
            WHERE d.tenant_id = @TenantId AND d.org_id = @OrgId AND d.is_archived = FALSE
            ORDER BY d.name
            """,
            new { TenantId = tenantId, OrgId = orgId });

        var mutable = rows.Select(r => new MutableNode
        {
            Id = r.Id.ToString(),
            Name = r.Name,
            ParentId = r.ParentDepartmentId?.ToString(),
            Head = r.HeadId is null ? null : new DepartmentHeadDto
            {
                EmployeeId = r.HeadId.Value.ToString(),
                FullName = NameFormatting.BuildFullName(r.HeadFirstName!, null, r.HeadLastName!),
                JobTitle = r.HeadJobTitle,
                Initials = NameFormatting.BuildInitials(r.HeadFirstName!, r.HeadLastName!),
            },
            EmployeeCount = r.EmployeeCount,
        }).ToDictionary(n => n.Id);

        foreach (var node in mutable.Values)
        {
            if (node.ParentId is not null && mutable.TryGetValue(node.ParentId, out var parent))
                parent.Children.Add(node);
        }

        var treeRoots = mutable.Values
            .Where(n => n.ParentId is null)
            .Select(n => n.ToDto())
            .OrderBy(n => n.Name)
            .ToList();

        return Respons<OrgChartDto>.Ok(new OrgChartDto { Roots = treeRoots });
    }

    public async Task<Respons<CreateDepartmentResponseDto>> CreateDepartmentAsync(
        CreateDepartmentRequestDto request,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Respons<CreateDepartmentResponseDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "Department name is required." });

        await using var connection = await _database.GetConnectionAsync(ct);

        var id = await connection.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_departments (tenant_id, org_id, name, parent_department_id, head_of_department_id)
            VALUES (@TenantId, @OrgId, @Name, @ParentDepartmentId, @HeadOfDepartmentId)
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Name = request.Name.Trim(),
                ParentDepartmentId = request.ParentDepartmentId,
                HeadOfDepartmentId = request.HeadOfDepartmentId,
            });

        return Respons<CreateDepartmentResponseDto>.Ok(new CreateDepartmentResponseDto
        {
            DepartmentId = id.ToString(),
            Name = request.Name.Trim(),
        });
    }

    public async Task<Respons<CreateDepartmentResponseDto>> UpdateDepartmentAsync(
        Guid id,
        UpdateDepartmentRequestDto request,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var exists = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1 FROM zeloshr.zhr_departments
                WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
            )
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!exists)
            return Respons<CreateDepartmentResponseDto>.Fail("Department not found.", statusCode: 404);

        if (request.ParentDepartmentId == id)
            return Respons<CreateDepartmentResponseDto>.ValidationError(
                new Dictionary<string, string> { ["parentDepartmentId"] = "Department cannot be its own parent." });

        var sets = new List<string>();
        var parameters = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            sets.Add("name = @Name");
            parameters.Add("Name", request.Name.Trim());
        }
        if (request.ParentDepartmentId.HasValue)
        {
            sets.Add("parent_department_id = @ParentDepartmentId");
            parameters.Add("ParentDepartmentId", request.ParentDepartmentId);
        }
        if (request.HeadOfDepartmentId.HasValue)
        {
            sets.Add("head_of_department_id = @HeadOfDepartmentId");
            parameters.Add("HeadOfDepartmentId", request.HeadOfDepartmentId);
        }

        if (sets.Count == 0)
            return Respons<CreateDepartmentResponseDto>.Fail("No fields to update.", statusCode: 400);

        sets.Add("updated_at = NOW()");
        var name = await connection.QuerySingleAsync<string>(
            $"""
            UPDATE zeloshr.zhr_departments SET {string.Join(", ", sets)}
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            RETURNING name
            """,
            parameters);

        return Respons<CreateDepartmentResponseDto>.Ok(new CreateDepartmentResponseDto
        {
            DepartmentId = id.ToString(),
            Name = name,
        });
    }

    public async Task<Respons<object>> ArchiveDepartmentAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(
            """
            UPDATE zeloshr.zhr_departments
            SET is_archived = TRUE, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (affected == 0)
            return Respons<object>.Fail("Department not found.", statusCode: 404);
        return Respons<object>.Ok(new { departmentId = id.ToString() }, "Department archived.");
    }

    public async Task<Respons<BranchMutationResponseDto>> CreateBranchAsync(
        CreateBranchRequestDto request,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Respons<BranchMutationResponseDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "Branch name is required." });

        await using var connection = await _database.GetConnectionAsync(ct);
        try
        {
            var id = await connection.QuerySingleAsync<Guid>(
                """
                INSERT INTO zeloshr.zhr_branches (tenant_id, org_id, name)
                VALUES (@TenantId, @OrgId, @Name)
                RETURNING id
                """,
                new { TenantId = tenantId, OrgId = orgId, Name = request.Name.Trim() });

            return Respons<BranchMutationResponseDto>.Ok(new BranchMutationResponseDto
            {
                BranchId = id.ToString(),
                Name = request.Name.Trim(),
            });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
        {
            return Respons<BranchMutationResponseDto>.Fail("A branch with this name already exists.", statusCode: 409);
        }
    }

    public async Task<Respons<BranchMutationResponseDto>> UpdateBranchAsync(
        Guid id,
        UpdateBranchRequestDto request,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Respons<BranchMutationResponseDto>.Fail("No fields to update.", statusCode: 400);

        await using var connection = await _database.GetConnectionAsync(ct);
        var name = await connection.QuerySingleOrDefaultAsync<string>(
            """
            UPDATE zeloshr.zhr_branches
            SET name = @Name, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
            RETURNING name
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId, Name = request.Name!.Trim() });

        if (name is null)
            return Respons<BranchMutationResponseDto>.Fail("Branch not found.", statusCode: 404);

        return Respons<BranchMutationResponseDto>.Ok(new BranchMutationResponseDto
        {
            BranchId = id.ToString(),
            Name = name,
        });
    }

    public async Task<Respons<object>> ArchiveBranchAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(
            """
            UPDATE zeloshr.zhr_branches
            SET is_archived = TRUE, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (affected == 0)
            return Respons<object>.Fail("Branch not found.", statusCode: 404);
        return Respons<object>.Ok(new { branchId = id.ToString() }, "Branch archived.");
    }

    private sealed class DepartmentChartRow
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public Guid? ParentDepartmentId { get; init; }
        public bool IsArchived { get; init; }
        public Guid? HeadId { get; init; }
        public string? HeadFirstName { get; init; }
        public string? HeadLastName { get; init; }
        public string? HeadJobTitle { get; init; }
        public int EmployeeCount { get; init; }
    }

    private sealed class MutableNode
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? ParentId { get; init; }
        public DepartmentHeadDto? Head { get; init; }
        public int EmployeeCount { get; init; }
        public List<MutableNode> Children { get; } = [];

        public OrgChartNodeDto ToDto() => new()
        {
            Id = Id,
            Name = Name,
            NodeType = "department",
            ParentId = ParentId,
            HeadOfDepartment = Head,
            EmployeeCount = EmployeeCount,
            Children = Children.Select(c => c.ToDto()).OrderBy(c => c.Name).ToList(),
        };
    }
}
