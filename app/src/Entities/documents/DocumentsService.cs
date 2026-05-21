using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Documents;

public class DocumentsService
{
    private readonly IDocumentsRepository _documents;
    private readonly IEmployeeRepository _employees;

    public DocumentsService(IDocumentsRepository documents, IEmployeeRepository employees)
    {
        _documents = documents;
        _employees = employees;
    }

    public async Task<Respons<DocumentsSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct)
    {
        var summary = await _documents.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<DocumentsSummaryDto>.Ok(summary);
    }

    public async Task<Respons<DocumentListDto>> ListAsync(
        string? search, string? category, Guid? employeeId,
        int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _documents.ListScopedAsync(
            tenantId, orgId, search, category, employeeId, paging.Page, paging.Size, ct);
        var summary = await _documents.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<DocumentListDto>.Ok(
            new DocumentListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<DocumentListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<DocumentListItemDto>> CreateAsync(
        CreateDocumentDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<DocumentListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Active" : data.Status.Trim();
        var fullName = NameFormatting.ResolveFullName(emp.FullName, emp.FirstName, emp.MiddleName, emp.LastName);

        var id = await _documents.CreateScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            fullName,
            data.DocumentName!.Trim(),
            data.Category!.Trim(),
            data.FileSizeKb,
            data.UploadedBy!.Trim(),
            status,
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<DocumentListItemDto>> UpdateAsync(
        Guid id, UpdateDocumentDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasDocName = !string.IsNullOrWhiteSpace(data.DocumentName);
        var hasCategory = !string.IsNullOrWhiteSpace(data.Category);
        var hasFileSize = data.FileSizeKb.HasValue;
        var hasUploadedBy = !string.IsNullOrWhiteSpace(data.UploadedBy);
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        if (!hasDocName && !hasCategory && !hasFileSize && !hasUploadedBy && !hasStatus)
            return Respons<DocumentListItemDto>.Fail("No fields to update.", statusCode: 400);

        var updated = await _documents.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasDocName ? data.DocumentName : null,
            hasCategory ? data.Category : null,
            hasFileSize ? data.FileSizeKb : null,
            hasUploadedBy ? data.UploadedBy : null,
            hasStatus ? data.Status : null,
            ct);

        if (updated is null)
        {
            var exists = await _documents.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<DocumentListItemDto>.Fail("Document not found.", statusCode: 404)
                : Respons<DocumentListItemDto>.Fail("No fields to update.", statusCode: 400);
        }

        return Respons<DocumentListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _documents.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Document not found.", statusCode: 404);
        return Respons<object>.Ok(new { documentId = id.ToString() }, "Document deleted.");
    }

    private async Task<Respons<DocumentListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _documents.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<DocumentListItemDto>.Fail("Document not found.", statusCode: 404)
            : Respons<DocumentListItemDto>.Ok(row);
    }
}
