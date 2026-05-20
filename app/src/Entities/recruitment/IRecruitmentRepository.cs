namespace ZelosHR.Api.Entities.Recruitment;

public interface IRecruitmentRepository
{
    Task<RecruitmentSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<JobPostingListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<JobPostingListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string title,
        string? departmentName,
        string? branchName,
        string? employmentType,
        string status,
        DateOnly postedAt,
        DateOnly? closingDate,
        CancellationToken ct = default);

    Task<JobPostingListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? title,
        string? departmentName,
        string? branchName,
        string? employmentType,
        DateOnly? postedAt,
        DateOnly? closingDate,
        string? status,
        int? applicantsCount,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
