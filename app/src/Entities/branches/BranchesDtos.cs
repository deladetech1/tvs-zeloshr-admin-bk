namespace ZelosHR.Api.Entities.Branches;

public sealed class BranchListItemDto
{
    public required string BranchId { get; init; }
    public required string Name { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? CountryCode { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsArchived { get; init; }
}

public sealed class BranchListDto
{
    public IReadOnlyList<BranchListItemDto> Items { get; init; } = [];
}
