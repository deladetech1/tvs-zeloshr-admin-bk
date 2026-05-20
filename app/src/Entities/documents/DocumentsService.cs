using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Documents;

public class DocumentsService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public DocumentsService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<DocumentsSummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var s = await c.QuerySingleAsync<DocumentsSummaryDto>(
            """
            SELECT
                COUNT(*)::int AS TotalDocuments,
                COUNT(*) FILTER (WHERE category = 'Contract')::int AS ContractDocuments,
                COUNT(*) FILTER (WHERE category = 'National ID')::int AS NationalIdDocuments,
                COUNT(*) FILTER (WHERE category = 'Certificate')::int AS CertificateDocuments
            FROM zeloshr.zhr_employee_documents WHERE tenant_id = @TenantId AND org_id = @OrgId
            """, new { TenantId = tenantId, OrgId = orgId });
        return Respons<DocumentsSummaryDto>.Ok(s);
    }

    public async Task<Respons<DocumentListDto>> ListAsync(
        string? search, string? category, Guid? employeeId,
        int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        await using var c = await _database.GetConnectionAsync(ct);
        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var p = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
        { conditions.Add("(employee_full_name ILIKE @Search OR document_name ILIKE @Search)"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(category) && category != "all") { conditions.Add("category = @Category"); p.Add("Category", category); }
        if (employeeId.HasValue) { conditions.Add("employee_id = @EmployeeId"); p.Add("EmployeeId", employeeId.Value); }
        var where = string.Join(" AND ", conditions);
        p.Add("Limit", paging.Size); p.Add("Offset", paging.Offset);
        var total = await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM zeloshr.zhr_employee_documents WHERE {where}", p);
        var items = (await c.QueryAsync<DocumentListItemDto>(
            $"""
            SELECT id::text AS DocumentId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   document_name AS DocumentName, category AS Category, file_size_kb AS FileSizeKb,
                   uploaded_by AS UploadedBy, uploaded_at AS UploadedAt, status AS Status
            FROM zeloshr.zhr_employee_documents WHERE {where} ORDER BY uploaded_at DESC LIMIT @Limit OFFSET @Offset
            """, p)).ToList();
        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new DocumentsSummaryDto();
        return Respons<DocumentListDto>.Ok(new DocumentListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta { Page = paging.Page, Size = paging.Size, Total = total, HasNext = paging.Offset + items.Count < total });
    }

    public async Task<Respons<DocumentListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<DocumentListItemDto>> CreateAsync(
        CreateDocumentDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<DocumentListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Active" : data.Status.Trim();

        await using var c = await _database.GetConnectionAsync(ct);
        var id = await c.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_employee_documents (
                tenant_id, org_id, employee_id, employee_full_name, document_name, category,
                file_size_kb, uploaded_by, status
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @FullName, @DocName, @Category,
                @FileSize, @UploadedBy, @Status
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                FullName = emp.FullName,
                DocName = data.DocumentName!.Trim(),
                Category = data.Category!.Trim(),
                FileSize = data.FileSizeKb,
                UploadedBy = data.UploadedBy!.Trim(),
                Status = status,
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<DocumentListItemDto>> UpdateAsync(
        Guid id, UpdateDocumentDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.DocumentName)) { sets.Add("document_name = @DocName"); p.Add("DocName", data.DocumentName.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Category)) { sets.Add("category = @Category"); p.Add("Category", data.Category.Trim()); }
        if (data.FileSizeKb.HasValue) { sets.Add("file_size_kb = @FileSize"); p.Add("FileSize", data.FileSizeKb.Value); }
        if (!string.IsNullOrWhiteSpace(data.UploadedBy)) { sets.Add("uploaded_by = @UploadedBy"); p.Add("UploadedBy", data.UploadedBy.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (sets.Count == 0)
            return Respons<DocumentListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var c = await _database.GetConnectionAsync(ct);
        if (await c.ExecuteAsync(
                $"UPDATE zeloshr.zhr_employee_documents SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<DocumentListItemDto>.Fail("Document not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var n = await c.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_employee_documents WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Document not found.", statusCode: 404)
            : Respons<object>.Ok(new { documentId = id.ToString() }, "Document deleted.");
    }

    private async Task<Respons<DocumentListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<DocumentListItemDto>(
            """
            SELECT id::text AS DocumentId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   document_name AS DocumentName, category AS Category, file_size_kb AS FileSizeKb,
                   uploaded_by AS UploadedBy, uploaded_at AS UploadedAt, status AS Status
            FROM zeloshr.zhr_employee_documents
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<DocumentListItemDto>.Fail("Document not found.", statusCode: 404)
            : Respons<DocumentListItemDto>.Ok(row);
    }
}
