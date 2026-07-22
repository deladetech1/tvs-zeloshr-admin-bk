using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.CompanyLocalization;

public class CompanyLocalizationService
{
    private readonly ICompanyLocalizationRepository _localization;
    private readonly ICpCurrencyRepository _currencies;
    private readonly ICpUserRepository _cpUsers;

    public CompanyLocalizationService(
        ICompanyLocalizationRepository localization,
        ICpCurrencyRepository currencies,
        ICpUserRepository cpUsers)
    {
        _localization = localization;
        _currencies = currencies;
        _cpUsers = cpUsers;
    }

    public async Task<Respons<CompanyLocalizationReadDto>> GetAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var currencyId = await ResolveDefaultCurrencyIdAsync(tenantId, ct);
        if (currencyId is null)
        {
            return Respons<CompanyLocalizationReadDto>.Fail(
                "No active currency is configured for this tenant. Configure currencies in Trovesuite before loading localization settings.",
                statusCode: 503);
        }

        var entity = await _localization.EnsureStubAsync(tenantId, orgId, currencyId, actorUserId, ct);
        return await BuildReadResponseAsync(entity, tenantId, ct);
    }

    public async Task<Respons<CompanyLocalizationReadDto>> CreateAsync(
        CreateCompanyLocalizationDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = await ValidateFieldsAsync(
            body.TimeZone, body.CurrencyId, body.DateFormat, body.NumberFormat,
            body.FirstDayOfWeek, body.YearStartMonth, body.YearStartDay, tenantId, ct);
        if (errors is not null)
            return Respons<CompanyLocalizationReadDto>.ValidationError(errors);

        var existing = await _localization.GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
        {
            return Respons<CompanyLocalizationReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["time_zone"] =
                    "Localization settings already exist for this organisation. Use PUT /company/localization/update instead.",
            });
        }

        var entity = await _localization.CreateAsync(tenantId, orgId, body, actorUserId, ct);
        return await BuildReadResponseAsync(entity, tenantId, ct);
    }

    public async Task<Respons<CompanyLocalizationReadDto>> UpdateAsync(
        UpdateCompanyLocalizationDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = MergeErrors(
            ValidateId(body.Id),
            await ValidateFieldsAsync(
                body.TimeZone, body.CurrencyId, body.DateFormat, body.NumberFormat,
                body.FirstDayOfWeek, body.YearStartMonth, body.YearStartDay, tenantId, ct));
        if (errors is not null)
            return Respons<CompanyLocalizationReadDto>.ValidationError(errors);

        var existing = await _localization.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<CompanyLocalizationReadDto>.Fail("Localization settings not found.", statusCode: 404);

        if (!Guid.TryParse(body.Id, out var parsedId) || parsedId != existing.Id)
        {
            return Respons<CompanyLocalizationReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current localization settings.",
            });
        }

        var updated = await _localization.UpdateAsync(tenantId, orgId, body, actorUserId, ct);
        if (updated is null)
            return Respons<CompanyLocalizationReadDto>.Fail("Localization settings not found.", statusCode: 404);

        return await BuildReadResponseAsync(updated, tenantId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        string tenantId, string orgId, Guid id, CancellationToken ct = default)
    {
        var existing = await _localization.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<object>.Fail("Localization settings not found.", statusCode: 404);

        if (id != existing.Id)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current localization settings.",
            });
        }

        var deleted = await _localization.DeleteAsync(tenantId, orgId, ct);
        if (!deleted)
            return Respons<object>.Fail("Localization settings not found.", statusCode: 404);

        return Respons<object>.Ok(new { }, detail: "Localization settings deleted.");
    }

    private async Task<string?> ResolveDefaultCurrencyIdAsync(string tenantId, CancellationToken ct)
    {
        var defaultCurrency = await _currencies.GetDefaultAsync(tenantId, ct);
        if (defaultCurrency is not null)
            return defaultCurrency.Id;

        var active = await _currencies.ListAsync(tenantId, isActive: true, ct);
        return active.Count > 0 ? active[0].Id : null;
    }

    private async Task<Respons<CompanyLocalizationReadDto>> BuildReadResponseAsync(
        CompanyLocalizationEntity entity, string tenantId, CancellationToken ct)
    {
        var users = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(new[] { new[] { entity.CreatedBy, entity.UpdatedBy } }),
            tenantId, ct);

        var dto = new CompanyLocalizationReadDto
        {
            Id = entity.Id.ToString(),
            TimeZone = entity.TimeZone,
            CurrencyId = entity.CurrencyId,
            DateFormat = entity.DateFormat,
            NumberFormat = entity.NumberFormat,
            FirstDayOfWeek = entity.FirstDayOfWeek,
            YearStartMonth = entity.YearStartMonth,
            YearStartDay = entity.YearStartDay,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedById = entity.CreatedBy,
            UpdatedById = entity.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(entity.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(entity.UpdatedBy, users),
        };

        return Respons<CompanyLocalizationReadDto>.Ok(dto);
    }

    private async Task<Dictionary<string, string>?> ValidateFieldsAsync(
        string? timeZone,
        string? currencyId,
        string? dateFormat,
        string? numberFormat,
        string? firstDayOfWeek,
        string? yearStartMonth,
        int? yearStartDay,
        string tenantId,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(timeZone))
            errors["time_zone"] = "Time zone is required.";
        else if (!IsValidTimeZone(timeZone))
            errors["time_zone"] = "Time zone must be a valid IANA time zone id (e.g. Africa/Accra).";

        if (string.IsNullOrWhiteSpace(currencyId))
            errors["currency_id"] = "Currency is required.";
        else if (!await _currencies.ExistsAsync(currencyId.Trim(), tenantId, ct))
            errors["currency_id"] = "Currency not found for this tenant.";

        AddRequiredLengthError(errors, "date_format", dateFormat, 50);
        AddRequiredLengthError(errors, "number_format", numberFormat, 50);
        AddRequiredLengthError(errors, "first_day_of_week", firstDayOfWeek, 20);
        AddRequiredLengthError(errors, "year_start_month", yearStartMonth, 20);

        if (yearStartDay is null)
            errors["year_start_day"] = "Year start day is required.";
        else if (yearStartDay is < 1 or > 31)
            errors["year_start_day"] = "Year start day must be between 1 and 31.";

        return errors.Count == 0 ? null : errors;
    }

    private static bool IsValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static Dictionary<string, string>? ValidateId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new Dictionary<string, string> { ["id"] = "Localization settings id is required." };
        if (!Guid.TryParse(id, out _))
            return new Dictionary<string, string> { ["id"] = "Localization settings id must be a valid UUID." };
        return null;
    }

    private static Dictionary<string, string>? MergeErrors(
        Dictionary<string, string>? first, Dictionary<string, string>? second)
    {
        if (first is null)
            return second;
        if (second is null)
            return first;
        foreach (var (key, value) in second)
            first[key] = value;
        return first;
    }

    private static void AddRequiredLengthError(
        Dictionary<string, string> errors, string key, string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[key] = $"{key} is required.";
        else if (value.Trim().Length > max)
            errors[key] = $"{key} must be at most {max} characters.";
    }
}
