using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class LeaveRepository(ZelosHrDbContext db) : ILeaveRepository
{
    private IQueryable<LeaveRequestEntity> Requests(string tenantId, string orgId) =>
        db.LeaveRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.OrgId == orgId);

    private IQueryable<LeaveBalanceEntity> Balances(string tenantId, string orgId) =>
        db.LeaveBalances.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId);

    public async Task<LeaveSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var query = Requests(tenantId, orgId);

        return new LeaveSummaryDto
        {
            PendingRequests = await query.CountAsync(r => r.Status == LeaveRequestStatuses.Pending, ct),
            ApprovedThisMonth = await query.CountAsync(
                r => r.Status == LeaveRequestStatuses.Approved
                     && DateOnly.FromDateTime(r.SubmittedAt.UtcDateTime) >= monthStart
                     && DateOnly.FromDateTime(r.SubmittedAt.UtcDateTime) < monthEnd,
                ct),
            OnLeaveToday = await query.CountAsync(
                r => r.Status == LeaveRequestStatuses.Approved && r.StartDate <= today && r.EndDate >= today, ct),
            TotalRequests = await query.CountAsync(ct),
        };
    }

    public async Task<int> CountEmployeeRequestsScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string? status,
        DateOnly? fromDate,
        CancellationToken ct = default)
    {
        var query = Requests(tenantId, orgId).Where(r => r.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);
        if (fromDate.HasValue)
            query = query.Where(r => r.StartDate >= fromDate.Value);
        return await query.CountAsync(ct);
    }

    public async Task<(IReadOnlyList<LeaveRequestListItemDto> Requests, int Total)> ListRequestsScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        Guid? leaveTypeId,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Requests(tenantId, orgId);
        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
            query = query.Where(r => EF.Functions.ILike(r.EmployeeFullName, $"%{search.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status == status.Trim());
        if (leaveTypeId.HasValue)
            query = query.Where(r => r.LeaveTypeId == leaveTypeId.Value);

        var total = await query.CountAsync(ct);
        var entities = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var remainingByKey = await LoadRemainingLookupAsync(
            tenantId, orgId, entities.Where(r => r.LeaveTypeId.HasValue).Select(r => (r.EmployeeId, r.LeaveTypeId!.Value)), ct);
        var requests = entities
            .Select(r => ToRequestDto(
                r,
                r.LeaveTypeId.HasValue
                    ? remainingByKey.GetValueOrDefault((r.EmployeeId, r.LeaveTypeId.Value))
                    : null))
            .ToList();

        return (requests, total);
    }

    public async Task<LeaveRequestListItemDto?> GetRequestByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Requests(tenantId, orgId).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null)
            return null;

        decimal? remaining = null;
        if (entity.LeaveTypeId.HasValue)
        {
            var balance = await GetBalanceForEmployeeScopedAsync(
                tenantId, orgId, entity.EmployeeId, entity.LeaveTypeId.Value, ct);
            remaining = balance?.RemainingDays;
        }

        return ToRequestDto(entity, remaining);
    }

    public async Task<Guid> CreateRequestScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        Guid leaveTypeId,
        string leaveTypeName,
        DateOnly startDate,
        DateOnly endDate,
        decimal daysRequested,
        string? notes,
        CancellationToken ct = default)
    {
        var entity = new LeaveRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            LeaveTypeId = leaveTypeId,
            LeaveType = leaveTypeName,
            StartDate = startDate,
            EndDate = endDate,
            DaysRequested = daysRequested,
            Status = LeaveRequestStatuses.Pending,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            SubmittedAt = DateTimeOffset.UtcNow,
        };
        db.LeaveRequests.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveRequestListItemDto?> UpdateRequestScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? notes,
        CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
            changed = true;
        }
        if (notes is not null)
        {
            entity.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            changed = true;
        }

        if (!changed)
            return null;

        await db.SaveChangesAsync(ct);
        decimal? remaining = null;
        if (entity.LeaveTypeId.HasValue)
        {
            var balance = await GetBalanceForEmployeeScopedAsync(
                tenantId, orgId, entity.EmployeeId, entity.LeaveTypeId.Value, ct);
            remaining = balance?.RemainingDays;
        }

        return ToRequestDto(entity, remaining);
    }

    public async Task<LeaveRequestListItemDto?> ApproveRequestScopedAsync(
        Guid id, string tenantId, string orgId, string approverId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null || entity.Status != LeaveRequestStatuses.Pending)
            return null;

        LeaveBalanceEntity? balance = null;
        if (entity.LeaveTypeId.HasValue)
        {
            balance = await db.LeaveBalances.FirstOrDefaultAsync(
                b => b.TenantId == tenantId
                     && b.OrgId == orgId
                     && b.EmployeeId == entity.EmployeeId
                     && b.LeaveTypeId == entity.LeaveTypeId,
                ct);
            if (balance is not null)
            {
                balance.UsedDays += entity.DaysRequested;
                balance.RemainingDays = balance.EntitledDays - balance.UsedDays;
            }
        }

        entity.Status = LeaveRequestStatuses.Approved;
        entity.ApproverId = approverId.Trim();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToRequestDto(entity, balance?.RemainingDays);
    }

    public async Task<LeaveRequestListItemDto?> RejectRequestScopedAsync(
        Guid id, string tenantId, string orgId, string approverId, string? notes, CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null || entity.Status != LeaveRequestStatuses.Pending)
            return null;

        entity.Status = LeaveRequestStatuses.Rejected;
        entity.ApproverId = approverId.Trim();
        if (notes is not null)
            entity.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        await db.SaveChangesAsync(ct);
        decimal? remaining = null;
        if (entity.LeaveTypeId.HasValue)
        {
            var balance = await GetBalanceForEmployeeScopedAsync(
                tenantId, orgId, entity.EmployeeId, entity.LeaveTypeId.Value, ct);
            remaining = balance?.RemainingDays;
        }

        return ToRequestDto(entity, remaining);
    }

    public async Task<bool> DeletePendingRequestScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId && r.Status == LeaveRequestStatuses.Pending, ct);
        if (entity is null)
            return false;
        db.LeaveRequests.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<LeaveBalanceListItemDto>> ListBalancesScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        Guid? leaveTypeId,
        CancellationToken ct = default)
    {
        var query = Balances(tenantId, orgId);
        if (employeeId.HasValue)
            query = query.Where(b => b.EmployeeId == employeeId.Value);
        if (leaveTypeId.HasValue)
            query = query.Where(b => b.LeaveTypeId == leaveTypeId.Value);

        return await query
            .OrderBy(b => b.EmployeeFullName)
            .ThenBy(b => b.LeaveType)
            .Select(b => ToBalanceDto(b))
            .ToListAsync(ct);
    }

    public async Task<LeaveBalanceListItemDto?> GetBalanceScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Balances(tenantId, orgId).FirstOrDefaultAsync(b => b.Id == id, ct);
        return entity is null ? null : ToBalanceDto(entity);
    }

    public async Task<LeaveBalanceListItemDto?> GetBalanceForEmployeeScopedAsync(
        string tenantId, string orgId, Guid employeeId, Guid leaveTypeId, CancellationToken ct = default)
    {
        var entity = await Balances(tenantId, orgId).FirstOrDefaultAsync(
            b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId, ct);
        return entity is null ? null : ToBalanceDto(entity);
    }

    public async Task<Guid> CreateBalanceScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        Guid leaveTypeId,
        string leaveTypeName,
        decimal entitledDays,
        decimal usedDays,
        CancellationToken ct = default)
    {
        var entity = new LeaveBalanceEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            LeaveTypeId = leaveTypeId,
            LeaveType = leaveTypeName,
            EntitledDays = entitledDays,
            UsedDays = usedDays,
            RemainingDays = entitledDays - usedDays,
        };
        db.LeaveBalances.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveBalanceListItemDto?> UpdateBalanceScopedAsync(
        Guid id, string tenantId, string orgId, decimal? entitledDays, decimal? usedDays, CancellationToken ct = default)
    {
        var entity = await db.LeaveBalances.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (entitledDays.HasValue)
        {
            entity.EntitledDays = entitledDays.Value;
            changed = true;
        }
        if (usedDays.HasValue)
        {
            entity.UsedDays = usedDays.Value;
            changed = true;
        }

        if (!changed)
            return null;

        entity.RemainingDays = entity.EntitledDays - entity.UsedDays;
        await db.SaveChangesAsync(ct);
        return ToBalanceDto(entity);
    }

    public async Task<IReadOnlyList<LeaveTypeListItemDto>> ListTypesScopedAsync(
        string tenantId, string orgId, string? countryCode, bool activeOnly, CancellationToken ct = default)
    {
        var query = db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId);
        if (activeOnly)
            query = query.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(t => t.CountryCode == null || t.CountryCode == countryCode.Trim().ToUpperInvariant());

        return await query
            .OrderBy(t => t.Name)
            .Select(t => ToTypeDto(t))
            .ToListAsync(ct);
    }

    public async Task<LeaveTypeListItemDto?> GetTypeByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.Id == id)
            .Select(t => ToTypeDto(t))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> TypeNameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        var query = db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.Name == normalized);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task<Guid> CreateTypeScopedAsync(
        string tenantId, string orgId, CreateLeaveTypeDto data, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new LeaveTypeEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = data.Name!.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(data.CountryCode) ? null : data.CountryCode.Trim().ToUpperInvariant(),
            DefaultEntitledDays = data.DefaultEntitledDays,
            IsPaid = data.IsPaid,
            IsActive = data.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.LeaveTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveTypeListItemDto?> UpdateTypeScopedAsync(
        Guid id, string tenantId, string orgId, UpdateLeaveTypeDto data, CancellationToken ct = default)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            entity.Name = data.Name.Trim();
            changed = true;
        }
        if (data.CountryCode is not null)
        {
            entity.CountryCode = string.IsNullOrWhiteSpace(data.CountryCode)
                ? null
                : data.CountryCode.Trim().ToUpperInvariant();
            changed = true;
        }
        if (data.DefaultEntitledDays.HasValue)
        {
            entity.DefaultEntitledDays = data.DefaultEntitledDays.Value;
            changed = true;
        }
        if (data.IsPaid.HasValue)
        {
            entity.IsPaid = data.IsPaid.Value;
            changed = true;
        }
        if (data.IsActive.HasValue)
        {
            entity.IsActive = data.IsActive.Value;
            changed = true;
        }

        if (!changed)
            return null;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToTypeDto(entity);
    }

    public async Task<bool> DeleteTypeScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return false;

        var inUse = await db.LeaveRequests.AsNoTracking().AnyAsync(
            r => r.TenantId == tenantId && r.OrgId == orgId && r.LeaveTypeId == id, ct)
            || await db.LeaveBalances.AsNoTracking().AnyAsync(
                b => b.TenantId == tenantId && b.OrgId == orgId && b.LeaveTypeId == id, ct);
        if (inUse)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }

        db.LeaveTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(IReadOnlyList<PublicHolidayListItemDto> Items, int Total)> ListHolidaysScopedAsync(
        string tenantId,
        string orgId,
        string? countryCode,
        int? year,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.PublicHolidays.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.OrgId == orgId && h.IsActive);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(h => h.CountryCode == countryCode.Trim().ToUpperInvariant());
        if (year.HasValue)
            query = query.Where(h => h.HolidayDate.Year == year.Value || h.IsRecurring);
        if (branchId.HasValue)
            query = query.Where(h => h.BranchId == null || h.BranchId == branchId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(h => h.HolidayDate)
            .ThenBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => ToHolidayDto(h))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<PublicHolidayListItemDto?> GetHolidayByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.PublicHolidays.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.OrgId == orgId && h.Id == id)
            .Select(h => ToHolidayDto(h))
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> CreateHolidayScopedAsync(
        string tenantId, string orgId, CreatePublicHolidayDto data, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new PublicHolidayEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            CountryCode = data.CountryCode!.Trim().ToUpperInvariant(),
            Name = data.Name!.Trim(),
            HolidayDate = data.HolidayDate,
            IsRecurring = data.IsRecurring,
            BranchId = data.BranchId,
            IsActive = data.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.PublicHolidays.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<PublicHolidayListItemDto?> UpdateHolidayScopedAsync(
        Guid id, string tenantId, string orgId, UpdatePublicHolidayDto data, CancellationToken ct = default)
    {
        var entity = await db.PublicHolidays.FirstOrDefaultAsync(
            h => h.Id == id && h.TenantId == tenantId && h.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(data.CountryCode))
        {
            entity.CountryCode = data.CountryCode.Trim().ToUpperInvariant();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            entity.Name = data.Name.Trim();
            changed = true;
        }
        if (data.HolidayDate.HasValue)
        {
            entity.HolidayDate = data.HolidayDate.Value;
            changed = true;
        }
        if (data.IsRecurring.HasValue)
        {
            entity.IsRecurring = data.IsRecurring.Value;
            changed = true;
        }
        if (data.BranchId.HasValue)
        {
            entity.BranchId = data.BranchId;
            changed = true;
        }
        if (data.IsActive.HasValue)
        {
            entity.IsActive = data.IsActive.Value;
            changed = true;
        }

        if (!changed)
            return null;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToHolidayDto(entity);
    }

    public async Task<bool> DeleteHolidayScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.PublicHolidays.FirstOrDefaultAsync(
            h => h.Id == id && h.TenantId == tenantId && h.OrgId == orgId, ct);
        if (entity is null)
            return false;

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<Dictionary<(Guid EmployeeId, Guid LeaveTypeId), decimal>> LoadRemainingLookupAsync(
        string tenantId,
        string orgId,
        IEnumerable<(Guid EmployeeId, Guid LeaveTypeId)> keys,
        CancellationToken ct)
    {
        var keyList = keys.Distinct().ToList();
        if (keyList.Count == 0)
            return [];

        var employeeIds = keyList.Select(k => k.EmployeeId).Distinct().ToList();
        var leaveTypeIds = keyList.Select(k => k.LeaveTypeId).Distinct().ToList();
        var rows = await Balances(tenantId, orgId)
            .Where(b => employeeIds.Contains(b.EmployeeId) && b.LeaveTypeId.HasValue && leaveTypeIds.Contains(b.LeaveTypeId.Value))
            .Select(b => new { b.EmployeeId, LeaveTypeId = b.LeaveTypeId!.Value, b.RemainingDays })
            .ToListAsync(ct);

        return rows.ToDictionary(r => (r.EmployeeId, r.LeaveTypeId), r => r.RemainingDays);
    }

    private static LeaveRequestListItemDto ToRequestDto(LeaveRequestEntity r, decimal? remainingDays) => new()
    {
        LeaveRequestId = r.Id.ToString(),
        EmployeeId = r.EmployeeId.ToString(),
        LeaveTypeId = r.LeaveTypeId?.ToString() ?? string.Empty,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        DaysRequested = r.DaysRequested,
        Status = r.Status,
        ApproverId = r.ApproverId,
        Notes = r.Notes,
        RemainingDays = remainingDays,
        SubmittedAt = r.SubmittedAt,
    };

    private static LeaveBalanceListItemDto ToBalanceDto(LeaveBalanceEntity b) => new()
    {
        LeaveBalanceId = b.Id.ToString(),
        EmployeeId = b.EmployeeId.ToString(),
        LeaveTypeId = b.LeaveTypeId?.ToString() ?? string.Empty,
        EntitledDays = b.EntitledDays,
        UsedDays = b.UsedDays,
        RemainingDays = b.RemainingDays,
    };

    private static LeaveTypeListItemDto ToTypeDto(LeaveTypeEntity t) => new()
    {
        LeaveTypeId = t.Id.ToString(),
        Name = t.Name,
        CountryCode = t.CountryCode,
        DefaultEntitledDays = t.DefaultEntitledDays,
        IsPaid = t.IsPaid,
        IsActive = t.IsActive,
    };

    private static PublicHolidayListItemDto ToHolidayDto(PublicHolidayEntity h) => new()
    {
        HolidayId = h.Id.ToString(),
        CountryCode = h.CountryCode,
        Name = h.Name,
        HolidayDate = h.HolidayDate,
        IsRecurring = h.IsRecurring,
        BranchId = h.BranchId?.ToString(),
        IsActive = h.IsActive,
    };
}
