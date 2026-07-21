using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.CompanyInfo;

public class CompanyInfoService
{
    private readonly ICompanyProfileRepository _profiles;
    private readonly ICompanyOfficeRepository _offices;
    private readonly ICpUserRepository _cpUsers;
    private readonly HrDocumentPresignedUrlService _documentUrls;
    private readonly ZelosHrDbContext _db;

    public CompanyInfoService(
        ICompanyProfileRepository profiles,
        ICompanyOfficeRepository offices,
        ICpUserRepository cpUsers,
        HrDocumentPresignedUrlService documentUrls,
        ZelosHrDbContext db)
    {
        _profiles = profiles;
        _offices = offices;
        _cpUsers = cpUsers;
        _documentUrls = documentUrls;
        _db = db;
    }

    public async Task<Respons<CompanyInfoReadDto>> GetAsync(
        string tenantId, string orgId, string? actorUserId, CancellationToken ct = default)
    {
        var profile = await _profiles.EnsureStubAsync(tenantId, orgId, actorUserId, ct);
        return await BuildReadResponseAsync(profile, tenantId, ct);
    }

    public async Task<Respons<CompanyInfoReadDto>> CreateAsync(
        CreateCompanyInfoDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateCreate(body);
        if (errors is not null)
            return Respons<CompanyInfoReadDto>.ValidationError(errors);

        var existing = await _profiles.GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
        {
            if (CompanyProfileState.IsConfigured(existing))
            {
                return Respons<CompanyInfoReadDto>.ValidationError(new Dictionary<string, string>
                {
                    ["legal_name"] =
                        "A company profile already exists for this organisation. Use PUT /company/info/update instead.",
                });
            }

            return await UpdateAsync(
                new UpdateCompanyInfoDto
                {
                    Id = existing.Id.ToString(),
                    LegalName = body.LegalName,
                    TradingName = body.TradingName,
                    Industry = body.Industry,
                    CompanySize = body.CompanySize,
                    BusinessRegistrationNumber = body.BusinessRegistrationNumber,
                    Tin = body.Tin,
                    PrimaryWorkCountry = body.PrimaryWorkCountry,
                    CompanyEmail = body.CompanyEmail,
                    Website = body.Website,
                    LogoUrl = body.LogoUrl,
                    BannerUrl = body.BannerUrl,
                    Offices = body.Offices,
                },
                tenantId,
                orgId,
                actorUserId,
                ct);
        }

        var documentError = await ValidateLogoAndBannerAsync(body.LogoUrl, body.BannerUrl, ct);
        if (documentError is not null)
            return Respons<CompanyInfoReadDto>.ValidationError(documentError);

        if (body.Offices is { Count: > 0 })
        {
            var (_, unknownIds) = await _offices.ReplaceAllAsync(tenantId, orgId, body.Offices, actorUserId, ct);
            if (unknownIds.Count > 0)
            {
                return Respons<CompanyInfoReadDto>.ValidationError(new Dictionary<string, string>
                {
                    ["offices"] =
                        $"No office found for id(s): {string.Join(", ", unknownIds)}. Omit office_id when adding new offices.",
                });
            }
        }

        var profile = await _profiles.CreateAsync(tenantId, orgId, body, actorUserId, ct);
        return await BuildReadResponseAsync(profile, tenantId, ct);
    }

    public async Task<Respons<CompanyInfoReadDto>> UpdateAsync(
        UpdateCompanyInfoDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateUpdate(body);
        if (errors is not null)
            return Respons<CompanyInfoReadDto>.ValidationError(errors);

        var existing = await _profiles.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<CompanyInfoReadDto>.Fail("Company profile not found.", statusCode: 404);

        if (!Guid.TryParse(body.Id, out var parsedId) || parsedId != existing.Id)
        {
            return Respons<CompanyInfoReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current company profile.",
            });
        }

        var documentError = await ValidateLogoAndBannerAsync(body.LogoUrl, body.BannerUrl, ct);
        if (documentError is not null)
            return Respons<CompanyInfoReadDto>.ValidationError(documentError);

        if (body.Offices is not null)
        {
            var (_, unknownIds) = await _offices.ReplaceAllAsync(tenantId, orgId, body.Offices, actorUserId, ct);
            if (unknownIds.Count > 0)
            {
                return Respons<CompanyInfoReadDto>.ValidationError(new Dictionary<string, string>
                {
                    ["offices"] = $"No office found for id(s): {string.Join(", ", unknownIds)}.",
                });
            }
        }

        var updated = await _profiles.UpdateAsync(tenantId, orgId, body, actorUserId, ct);
        if (updated is null)
            return Respons<CompanyInfoReadDto>.Fail("Company profile not found.", statusCode: 404);

        return await BuildReadResponseAsync(updated, tenantId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var deleted = await _profiles.DeleteAsync(tenantId, orgId, ct);
        if (!deleted)
        {
            await transaction.RollbackAsync(ct);
            return Respons<object>.Fail("Company profile not found.", statusCode: 404);
        }

        await _offices.DeleteAllAsync(tenantId, orgId, ct);
        await transaction.CommitAsync(ct);
        return Respons<object>.Ok(new { }, detail: "Company profile deleted.");
    }

    private async Task<Respons<CompanyInfoReadDto>> BuildReadResponseAsync(
        CompanyProfileEntity profile, string tenantId, CancellationToken ct)
    {
        var offices = await _offices.ListAsync(profile.TenantId, profile.OrgId, ct);

        var userIds = ResourceAuditMapper.CollectUserIds(
            new[] { new[] { profile.CreatedBy, profile.UpdatedBy } }
                .Concat(offices.Select(o => new[] { o.CreatedBy, o.UpdatedBy })));
        var users = await _cpUsers.GetByIdsAsync(userIds, tenantId, ct);

        var logoUrl = await _documentUrls.ResolveDocumentReadAsync(profile.LogoDocumentId, ct);
        var bannerUrl = await _documentUrls.ResolveDocumentReadAsync(profile.BannerDocumentId, ct);

        var dto = new CompanyInfoReadDto
        {
            Id = profile.Id.ToString(),
            LegalName = profile.LegalName,
            TradingName = profile.TradingName,
            Industry = profile.Industry,
            CompanySize = profile.CompanySize,
            BusinessRegistrationNumber = profile.BusinessRegistrationNumber,
            Tin = profile.Tin,
            PrimaryWorkCountry = profile.PrimaryWorkCountry,
            CompanyEmail = profile.CompanyEmail,
            Website = profile.Website,
            LogoUrl = logoUrl,
            BannerUrl = bannerUrl,
            Offices = offices.Select(o => ToOfficeDto(o, users)).ToList(),
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            CreatedById = profile.CreatedBy,
            UpdatedById = profile.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(profile.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(profile.UpdatedBy, users),
        };

        return Respons<CompanyInfoReadDto>.Ok(dto);
    }

    private async Task<Dictionary<string, string>?> ValidateLogoAndBannerAsync(
        string? logoUrl, string? bannerUrl, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            var error = await _documentUrls.ValidateDocumentReferenceAsync("logo_url", logoUrl, ct);
            if (error is not null)
                return error;
        }

        if (!string.IsNullOrWhiteSpace(bannerUrl))
        {
            var error = await _documentUrls.ValidateDocumentReferenceAsync("banner_url", bannerUrl, ct);
            if (error is not null)
                return error;
        }

        return null;
    }

    private static CompanyOfficeReadDto ToOfficeDto(
        CompanyOfficeEntity o, IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            OfficeId = o.Id.ToString(),
            Name = o.Name,
            Country = o.Country,
            City = o.City,
            Phone = o.Phone,
            IsHeadOffice = o.IsHeadOffice,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            CreatedById = o.CreatedBy,
            UpdatedById = o.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(o.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(o.UpdatedBy, users),
        };

    private static Dictionary<string, string>? ValidateCreate(CreateCompanyInfoDto body) =>
        ValidateProfileFields(
            body.LegalName, body.TradingName, body.Industry, body.CompanySize,
            body.BusinessRegistrationNumber, body.Tin, body.PrimaryWorkCountry,
            body.CompanyEmail, body.Website, body.Offices);

    /// <summary>Same field rules as create — update is a full replacement, not a partial patch.</summary>
    private static Dictionary<string, string>? ValidateUpdate(UpdateCompanyInfoDto body)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(body.Id))
            errors["id"] = "Company profile id is required.";
        else if (!Guid.TryParse(body.Id, out _))
            errors["id"] = "Company profile id must be a valid UUID.";

        var fieldErrors = ValidateProfileFields(
            body.LegalName, body.TradingName, body.Industry, body.CompanySize,
            body.BusinessRegistrationNumber, body.Tin, body.PrimaryWorkCountry,
            body.CompanyEmail, body.Website, body.Offices);
        MergeErrors(errors, fieldErrors);

        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string>? ValidateProfileFields(
        string? legalName,
        string? tradingName,
        string? industry,
        string? companySize,
        string? businessRegistrationNumber,
        string? tin,
        string? primaryWorkCountry,
        string? companyEmail,
        string? website,
        List<CompanyOfficeWriteDto>? offices)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(legalName))
            errors["legal_name"] = "Legal name is required.";
        else if (legalName.Trim().Length > 200)
            errors["legal_name"] = "Legal name must be at most 200 characters.";

        AddLengthError(errors, "trading_name", tradingName, 200);
        AddLengthError(errors, "industry", industry, 150);
        AddLengthError(errors, "company_size", companySize, 50);
        AddLengthError(errors, "business_registration_number", businessRegistrationNumber, 100);
        AddLengthError(errors, "tin", tin, 100);
        AddLengthError(errors, "primary_work_country", primaryWorkCountry, 100);
        AddLengthError(errors, "company_email", companyEmail, 200);
        AddLengthError(errors, "website", website, 300);

        MergeErrors(errors, ValidateOffices(offices));
        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string>? ValidateOffices(List<CompanyOfficeWriteDto>? offices)
    {
        if (offices is null || offices.Count == 0)
            return null;

        var errors = new Dictionary<string, string>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < offices.Count; i++)
        {
            var item = offices[i];
            if (string.IsNullOrWhiteSpace(item.Name))
                errors[$"offices[{i}].name"] = "Office name is required.";
            else if (item.Name.Trim().Length > 150)
                errors[$"offices[{i}].name"] = "Office name must be at most 150 characters.";
            else if (!seenNames.Add(item.Name.Trim()))
                errors[$"offices[{i}].name"] = "Duplicate office name in this request.";

            if (item.Country is { Length: > 100 })
                errors[$"offices[{i}].country"] = "Country must be at most 100 characters.";
            if (item.City is { Length: > 100 })
                errors[$"offices[{i}].city"] = "City must be at most 100 characters.";
            if (item.Phone is { Length: > 50 })
                errors[$"offices[{i}].phone"] = "Phone must be at most 50 characters.";
        }

        return errors.Count == 0 ? null : errors;
    }

    private static void MergeErrors(Dictionary<string, string> into, Dictionary<string, string>? from)
    {
        if (from is null)
            return;
        foreach (var (key, value) in from)
            into[key] = value;
    }

    private static void AddLengthError(Dictionary<string, string> errors, string key, string? value, int max)
    {
        if (value is { Length: > 0 } && value.Trim().Length > max)
            errors[key] = $"{key} must be at most {max} characters.";
    }
}
