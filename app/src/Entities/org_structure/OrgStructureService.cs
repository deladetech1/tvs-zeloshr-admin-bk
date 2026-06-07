using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.OrgStructure;

public class OrgStructureService
{
    private readonly DepartmentsService _departments;
    private readonly BranchesService _branches;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IOrgChartRepository _orgChartRepo;
    private readonly IBranchRepository _branchRepo;
    private readonly ICpUserRepository _cpUsers;
    private readonly HrDocumentPresignedUrlService _profileUrls;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrgStructureService(
        DepartmentsService departments,
        BranchesService branches,
        IDepartmentRepository departmentRepo,
        IOrgChartRepository orgChartRepo,
        IBranchRepository branchRepo,
        ICpUserRepository cpUsers,
        HrDocumentPresignedUrlService profileUrls,
        IHttpContextAccessor httpContextAccessor)
    {
        _departments = departments;
        _branches = branches;
        _departmentRepo = departmentRepo;
        _orgChartRepo = orgChartRepo;
        _branchRepo = branchRepo;
        _cpUsers = cpUsers;
        _profileUrls = profileUrls;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? CurrentUserId =>
        _httpContextAccessor.HttpContext?.Items[TrovesuiteHttpContextKeys.UserId] as string;

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
        var (employees, departmentHeads) =
            await _orgChartRepo.GetReportingHierarchyScopedAsync(tenantId, orgId, ct);

        var departmentByHeadId = departmentHeads
            .GroupBy(d => d.HeadOfDepartmentId)
            .ToDictionary(g => g.Key, g => g.First());

        var userIds = employees
            .Select(e => e.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct();
        var platformUsers = await _cpUsers.GetByIdsAsync(userIds, tenantId, ct);

        var storedProfileRefs = employees
            .Select(e =>
            {
                platformUsers.TryGetValue(e.UserId ?? string.Empty, out var cp);
                return cp?.ProfilePic ?? e.ProfilePhotoUrl;
            })
            .ToList();
        var profileUrlMap = await _profileUrls.ResolveDocumentReadsAsync(storedProfileRefs, ct);

        var profileUrlsByEmployeeId = employees.ToDictionary(
            e => e.Id,
            e =>
            {
                platformUsers.TryGetValue(e.UserId ?? string.Empty, out var cp);
                var storedProfileRef = cp?.ProfilePic ?? e.ProfilePhotoUrl;
                return storedProfileRef is null
                    ? null
                    : profileUrlMap.GetValueOrDefault(storedProfileRef.Trim());
            });

        var roots = OrgChartBuilder.Build(employees, departmentByHeadId, profileUrlsByEmployeeId);

        return Respons<OrgChartDto>.Ok(new OrgChartDto { Roots = roots });
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

        var descriptionErrors = OrgStructureValidation.ValidateDepartmentDescription(request.Description);
        if (descriptionErrors is not null)
            return Respons<CreateDepartmentResponseDto>.ValidationError(descriptionErrors);

        var headcountErrors = OrgStructureValidation.ValidateHeadcountCapacity(request.HeadcountCapacity);
        if (headcountErrors is not null)
            return Respons<CreateDepartmentResponseDto>.ValidationError(headcountErrors);

        try
        {
            var id = await _departmentRepo.CreateScopedAsync(
                tenantId, orgId, request.Name, request.ParentDepartmentId, request.HeadOfDepartmentId,
                request.Description, request.HeadcountCapacity, CurrentUserId, ct);

            var created = await _departmentRepo.GetActiveScopedAsync(id, tenantId, orgId, ct);
            return Respons<CreateDepartmentResponseDto>.Ok(
                await ToDepartmentMutationAsync(created!, tenantId, ct));
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
        var hasDescription = request.Description is not null;
        var hasHeadcountCapacity = request.HeadcountCapacity.HasValue;
        if (!hasName && !hasParent && !hasHead && !hasDescription && !hasHeadcountCapacity)
            return Respons<CreateDepartmentResponseDto>.ValidationError(new Dictionary<string, string>
            {
                ["request"] = "Provide at least one of: name, parent_department_id, head_of_department_id, description, headcount_capacity.",
            });

        if (hasDescription)
        {
            var descriptionErrors = OrgStructureValidation.ValidateDepartmentDescription(request.Description);
            if (descriptionErrors is not null)
                return Respons<CreateDepartmentResponseDto>.ValidationError(descriptionErrors);
        }

        if (hasHeadcountCapacity)
        {
            var headcountErrors = OrgStructureValidation.ValidateHeadcountCapacity(request.HeadcountCapacity);
            if (headcountErrors is not null)
                return Respons<CreateDepartmentResponseDto>.ValidationError(headcountErrors);
        }

        var name = await _departmentRepo.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasName ? request.Name : null,
            hasParent ? request.ParentDepartmentId : null,
            hasHead ? request.HeadOfDepartmentId : null,
            request.Description,
            hasDescription,
            request.HeadcountCapacity,
            hasHeadcountCapacity,
            CurrentUserId,
            ct);

        if (name is null)
            return Respons<CreateDepartmentResponseDto>.Fail("Department not found.", statusCode: 404);
        if (name.Length == 0)
            return Respons<CreateDepartmentResponseDto>.EmptyUpdateRequest();

        var updated = await _departmentRepo.GetActiveScopedAsync(id, tenantId, orgId, ct);
        return Respons<CreateDepartmentResponseDto>.Ok(
            await ToDepartmentMutationAsync(updated!, tenantId, ct));
    }

    public async Task<Respons<object>> DeleteDepartmentAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var result = await _departmentRepo.DeleteScopedAsync(id, tenantId, orgId, ct);
        return MapDepartmentDeleteResult(result, id);
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

        var locationErrors = OrgStructureValidation.ValidateBranchFields(
            request.Address, request.Country, request.Description);
        if (locationErrors is not null)
            return Respons<BranchMutationResponseDto>.ValidationError(locationErrors);

        try
        {
            var model = new BranchWriteModel(
                request.Name,
                request.Address,
                request.Country,
                request.Description);
            var id = await _branchRepo.CreateScopedAsync(model, tenantId, orgId, CurrentUserId, ct);
            var created = await _branchRepo.GetActiveScopedAsync(id, tenantId, orgId, ct);
            return Respons<BranchMutationResponseDto>.Ok(
                await ToBranchMutationAsync(created!, tenantId, ct));
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
        var hasName = !string.IsNullOrWhiteSpace(request.Name);
        var hasAddress = request.Address is not null;
        var hasCountry = request.Country is not null;
        var hasDescription = request.Description is not null;
        if (!hasName && !hasAddress && !hasCountry && !hasDescription)
        {
            return Respons<BranchMutationResponseDto>.ValidationError(new Dictionary<string, string>
            {
                ["request"] = "Provide at least one of: name, address, country, description.",
            });
        }

        if (hasName && string.IsNullOrWhiteSpace(request.Name))
        {
            return Respons<BranchMutationResponseDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "Branch name cannot be empty.",
            });
        }

        var locationErrors = OrgStructureValidation.ValidateBranchFields(
            hasAddress ? request.Address : null,
            hasCountry ? request.Country : null,
            hasDescription ? request.Description : null);
        if (locationErrors is not null)
            return Respons<BranchMutationResponseDto>.ValidationError(locationErrors);

        if (!await _branchRepo.ExistsActiveScopedAsync(id, tenantId, orgId, ct))
            return Respons<BranchMutationResponseDto>.Fail("Branch not found.", statusCode: 404);

        var updated = await _branchRepo.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            request.Name,
            request.Address,
            request.Country,
            request.Description,
            hasName,
            hasAddress,
            hasCountry,
            hasDescription,
            CurrentUserId,
            ct);

        if (updated is null)
            return Respons<BranchMutationResponseDto>.Fail("Branch not found.", statusCode: 404);

        return Respons<BranchMutationResponseDto>.Ok(await ToBranchMutationAsync(updated, tenantId, ct));
    }

    private async Task<CreateDepartmentResponseDto> ToDepartmentMutationAsync(
        DepartmentListRow row,
        string tenantId,
        CancellationToken ct)
    {
        var users = await _cpUsers.GetByIdsAsync(OrgStructureMapper.CollectUserIds([row]), tenantId, ct);
        return OrgStructureMapper.ToDepartmentMutation(row, users);
    }

    private async Task<BranchMutationResponseDto> ToBranchMutationAsync(
        BranchListRow row,
        string tenantId,
        CancellationToken ct)
    {
        var users = await _cpUsers.GetByIdsAsync(OrgStructureMapper.CollectUserIds([row]), tenantId, ct);
        return OrgStructureMapper.ToBranchMutation(row, users);
    }

    public async Task<Respons<object>> DeleteBranchAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var result = await _branchRepo.DeleteScopedAsync(id, tenantId, orgId, ct);
        return MapBranchDeleteResult(result, id);
    }

    private static Respons<object> MapDepartmentDeleteResult(OrgStructureDeleteResult result, Guid id) =>
        result switch
        {
            OrgStructureDeleteResult.Deleted => Respons<object>.Ok(
                new { departmentId = id.ToString() },
                "Department deleted."),
            OrgStructureDeleteResult.NotFound => Respons<object>.Fail(
                "Department not found.",
                statusCode: 404),
            OrgStructureDeleteResult.InUseByEmployees => Respons<object>.Fail(
                "Department cannot be deleted while employees are assigned to it.",
                statusCode: 409),
            OrgStructureDeleteResult.HasChildDepartments => Respons<object>.Fail(
                "Department cannot be deleted while it has child departments. Delete or reassign child departments first.",
                statusCode: 409),
            _ => Respons<object>.Fail("Delete failed.", statusCode: 500),
        };

    private static Respons<object> MapBranchDeleteResult(OrgStructureDeleteResult result, Guid id) =>
        result switch
        {
            OrgStructureDeleteResult.Deleted => Respons<object>.Ok(
                new { branchId = id.ToString() },
                "Branch deleted."),
            OrgStructureDeleteResult.NotFound => Respons<object>.Fail(
                "Branch not found.",
                statusCode: 404),
            OrgStructureDeleteResult.InUseByEmployees => Respons<object>.Fail(
                "Branch cannot be deleted while employees are assigned to it.",
                statusCode: 409),
            _ => Respons<object>.Fail("Delete failed.", statusCode: 500),
        };
}
