namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to human_resource.hr_document_paths (Trovesuite file registry).</summary>
public sealed class HrDocumentPathEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string DocumentPath { get; set; } = default!;
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public string DeleteStatus { get; set; } = "NOT_DELETED";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? Cdatetime { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
