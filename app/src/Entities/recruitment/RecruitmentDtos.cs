namespace ZelosHR.Api.Entities.Recruitment;

public sealed class RecruitmentSummaryDto
{
    public int OpenPostings { get; init; }
    public int TotalApplicants { get; init; }
    public int ClosingSoon { get; init; }
    public int ClosedPostings { get; init; }
}

public sealed class JobPostingListItemDto
{
    public required string JobPostingId { get; init; }
    public required string Title { get; init; }
    public string? DepartmentName { get; init; }
    public string? BranchName { get; init; }
    public string? EmploymentType { get; init; }
    public required string Status { get; init; }
    public int ApplicantsCount { get; init; }
    public DateOnly PostedAt { get; init; }
    public DateOnly? ClosingDate { get; init; }
}

public sealed class JobPostingListDto
{
    public RecruitmentSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<JobPostingListItemDto> Items { get; init; } = [];
}
