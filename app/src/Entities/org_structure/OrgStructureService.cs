using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.OrgStructure;

public class OrgStructureService
{
    private readonly DepartmentsService _departments;
    private readonly BranchesService _branches;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IBranchRepository _branchRepo;

    public OrgStructureService(
        DepartmentsService departments,
        BranchesService branches,
        IDepartmentRepository departmentRepo,
        IBranchRepository branchRepo)
    {
        _departments = departments;
        _branches = branches;
        _departmentRepo = departmentRepo;
        _branchRepo = branchRepo;
    }

    public Task<Respons<OrganisationSummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct) =>
        _departments.GetSummaryAsync(tenantId, orgId, ct);

    public Task<Respons<DepartmentListDto>> ListDepartmentsAsync(
        string? search, string sortBy, string sortOrder, bool includeArchived,
        int page, int size, string tenantId, string orgId, CancellationToken ct) =>
        _departments.ListDepartmentsAsync(search, sortBy, sortOrder, includeArchived, page, size, tenantId, orgId, ct);

    public Task<Respons<BranchListDto>> ListBranchesAsync(
        string? search,
        bool includeArchived,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct) =>
        _branches.ListBranchesAsync(tenantId, orgId, search, includeArchived, page, size, ct);

    public async Task<Respons<OrgChartDto>> GetOrgChartAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var rows = await _departmentRepo.GetOrgChartScopedAsync(tenantId, orgId, ct);

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

        try
        {
            var id = await _departmentRepo.CreateScopedAsync(
                tenantId, orgId, request.Name, request.ParentDepartmentId, request.HeadOfDepartmentId, ct);

            return Respons<CreateDepartmentResponseDto>.Ok(new CreateDepartmentResponseDto
            {
                DepartmentId = id.ToString(),
                Name = request.Name.Trim(),
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Respons<CreateDepartmentResponseDto>.Fail(
                "A department with this name already exists.", statusCode: 409);
        }
    }

    public async Task<Respons<CreateDepartmentResponseDto>> UpdateDepartmentAsync(
        Guid id,
        UpdateDepartmentRequestDto request,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (!await _departmentRepo.ExistsActiveScopedAsync(id, tenantId, orgId, ct))
            return Respons<CreateDepartmentResponseDto>.Fail("Department not found.", statusCode: 404);

        if (request.ParentDepartmentId == id)
            return Respons<CreateDepartmentResponseDto>.ValidationError(
                new Dictionary<string, string> { ["parent_department_id"] = "Department cannot be its own parent." });

        var hasName = !string.IsNullOrWhiteSpace(request.Name);
        var hasParent = request.ParentDepartmentId.HasValue;
        var hasHead = request.HeadOfDepartmentId.HasValue;
        if (!hasName && !hasParent && !hasHead)
            return Respons<CreateDepartmentResponseDto>.ValidationError(new Dictionary<string, string>
            {
                ["request"] = "Provide at least one of: name, parent_department_id, head_of_department_id.",
            });

        var name = await _departmentRepo.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasName ? request.Name : null,
            hasParent ? request.ParentDepartmentId : null,
            hasHead ? request.HeadOfDepartmentId : null,
            ct);

        if (name is null)
            return Respons<CreateDepartmentResponseDto>.Fail("Department not found.", statusCode: 404);
        if (name.Length == 0)
            return Respons<CreateDepartmentResponseDto>.EmptyUpdateRequest();

        return Respons<CreateDepartmentResponseDto>.Ok(new CreateDepartmentResponseDto
        {
            DepartmentId = id.ToString(),
            Name = name,
        });
    }

    public async Task<Respons<object>> ArchiveDepartmentAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _departmentRepo.ArchiveScopedAsync(id, tenantId, orgId, ct))
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

        try
        {
            var id = await _branchRepo.CreateScopedAsync(tenantId, orgId, request.Name, ct);
            return Respons<BranchMutationResponseDto>.Ok(new BranchMutationResponseDto
            {
                BranchId = id.ToString(),
                Name = request.Name.Trim(),
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
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
            return Respons<BranchMutationResponseDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "Branch name is required.",
            });

        var name = await _branchRepo.UpdateNameScopedAsync(id, tenantId, orgId, request.Name!, ct);
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
        if (!await _branchRepo.ArchiveScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Branch not found.", statusCode: 404);
        return Respons<object>.Ok(new { branchId = id.ToString() }, "Branch archived.");
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
