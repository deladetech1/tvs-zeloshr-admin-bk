using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Documents;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class DocumentsRepository(ZelosHrDbContext db) : IDocumentsRepository
{
    private IQueryable<EmployeeDocumentEntity> Scoped(string tenantId, string orgId) =>
        db.EmployeeDocuments.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId);

    public async Task<DocumentsSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        return new DocumentsSummaryDto
        {
            TotalDocuments = await query.CountAsync(ct),
            ContractDocuments = await query.CountAsync(d => d.Category == "Contract", ct),
            NationalIdDocuments = await query.CountAsync(d => d.Category == "National ID", ct),
            CertificateDocuments = await query.CountAsync(d => d.Category == "Certificate", ct),
        };
    }

    public async Task<(IReadOnlyList<DocumentListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? category,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId), search, category, employeeId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => ToDto(d))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<DocumentListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(d => d.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string documentName,
        string category,
        int fileSizeKb,
        string uploadedBy,
        string status,
        CancellationToken ct = default)
    {
        var entity = new EmployeeDocumentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            DocumentName = documentName,
            Category = category,
            FileSizeKb = fileSizeKb,
            UploadedBy = uploadedBy,
            UploadedAt = DateTimeOffset.UtcNow,
            Status = status,
        };
        db.EmployeeDocuments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<DocumentListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? documentName,
        string? category,
        int? fileSizeKb,
        string? uploadedBy,
        string? status,
        CancellationToken ct = default)
    {
        var entity = await db.EmployeeDocuments.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(documentName))
        {
            entity.DocumentName = documentName.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            entity.Category = category.Trim();
            changed = true;
        }
        if (fileSizeKb.HasValue)
        {
            entity.FileSizeKb = fileSizeKb.Value;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(uploadedBy))
        {
            entity.UploadedBy = uploadedBy.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
            changed = true;
        }

        if (!changed)
            return null;

        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.EmployeeDocuments.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.EmployeeDocuments.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<EmployeeDocumentEntity> ApplyFilters(
        IQueryable<EmployeeDocumentEntity> query,
        string? search,
        string? category,
        Guid? employeeId)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
        {
            var pattern = $"%{search}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.EmployeeFullName, pattern)
                || EF.Functions.ILike(d.DocumentName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
            query = query.Where(d => d.Category == category);

        if (employeeId.HasValue)
            query = query.Where(d => d.EmployeeId == employeeId.Value);

        return query;
    }

    private static DocumentListItemDto ToDto(EmployeeDocumentEntity d) => new()
    {
        DocumentId = d.Id.ToString(),
        EmployeeId = d.EmployeeId.ToString(),
        EmployeeFullName = d.EmployeeFullName ?? string.Empty,
        DocumentName = d.DocumentName ?? d.FileName,
        Category = d.Category,
        FileSizeKb = d.FileSizeKb ?? (int)Math.Max(1, d.FileSizeBytes / 1024),
        UploadedBy = d.UploadedBy ?? string.Empty,
        UploadedAt = d.UploadedAt,
        Status = d.Status ?? "Active",
    };
}
