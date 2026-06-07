namespace ZelosHR.Api.Entities.Branches;

public sealed class BranchListItemDto
{
    public required string BranchId { get; init; }
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? Description { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class BranchListDto
{
    public IReadOnlyList<BranchListItemDto> Items { get; init; } = [];
}
