using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.EmployeeIdFormat;

public sealed class EmployeeIdFormatService
{
    private readonly IEmployeeIdFormatRepository _formats;
    private readonly ICpUserRepository _cpUsers;
    private readonly EmployeeCodeGenerationService _codeGen;

    public EmployeeIdFormatService(
        IEmployeeIdFormatRepository formats,
        ICpUserRepository cpUsers,
        EmployeeCodeGenerationService codeGen)
    {
        _formats = formats;
        _cpUsers = cpUsers;
        _codeGen = codeGen;
    }

    public async Task<Respons<EmployeeIdFormatListDto>> ListAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await _formats.GetEntityAsync(tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeeIdFormatListDto>.Ok(new EmployeeIdFormatListDto { Items = Array.Empty<EmployeeIdFormatReadDto>() });

        var dto = await MapToReadDtoAsync(entity, tenantId, orgId, actorUserId, ct);
        return Respons<EmployeeIdFormatListDto>.Ok(new EmployeeIdFormatListDto { Items = new[] { dto } });
    }

    public async Task<Respons<EmployeeIdFormatReadDto>> GetAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await _formats.GetEntityAsync(tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeeIdFormatReadDto>.Fail("Employee ID format settings not found.", statusCode: 404);

        var dto = await MapToReadDtoAsync(entity, tenantId, orgId, actorUserId, ct);
        return Respons<EmployeeIdFormatReadDto>.Ok(dto);
    }

    public async Task<Respons<EmployeeIdFormatReadDto>> CreateAsync(
        CreateEmployeeIdFormatDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateFields(body.Prefix, body.DigitCount, body.StartingNumber, body.Separator, body.AutoGenerate);
        if (errors is not null)
            return Respons<EmployeeIdFormatReadDto>.ValidationError(errors);

        var existing = await _formats.GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
        {
            return Respons<EmployeeIdFormatReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["prefix"] =
                    "Employee ID format settings already exist for this organisation. Use PUT /company/id-format/update instead.",
            });
        }

        var entity = await _formats.CreateAsync(tenantId, orgId, body, actorUserId, ct);
        var dto = await MapToReadDtoAsync(entity, tenantId, orgId, actorUserId, ct);
        return Respons<EmployeeIdFormatReadDto>.Ok(dto);
    }

    public async Task<Respons<EmployeeIdFormatReadDto>> UpdateAsync(
        UpdateEmployeeIdFormatDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = MergeErrors(
            ValidateId(body.Id),
            ValidateFields(body.Prefix, body.DigitCount, body.StartingNumber, body.Separator, body.AutoGenerate));
        if (errors is not null)
            return Respons<EmployeeIdFormatReadDto>.ValidationError(errors);

        var existing = await _formats.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<EmployeeIdFormatReadDto>.Fail("Employee ID format settings not found.", statusCode: 404);

        if (!Guid.TryParse(body.Id, out var parsedId) || parsedId != existing.Id)
        {
            return Respons<EmployeeIdFormatReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current employee ID format settings.",
            });
        }

        var updated = await _formats.UpdateAsync(tenantId, orgId, body, actorUserId, ct);
        if (updated is null)
            return Respons<EmployeeIdFormatReadDto>.Fail("Employee ID format settings not found.", statusCode: 404);

        var dto = await MapToReadDtoAsync(updated, tenantId, orgId, actorUserId, ct);
        return Respons<EmployeeIdFormatReadDto>.Ok(dto);
    }

    public async Task<Respons<object>> DeleteAsync(
        string tenantId, string orgId, Guid id, CancellationToken ct = default)
    {
        var existing = await _formats.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<object>.Fail("Employee ID format settings not found.", statusCode: 404);

        if (id != existing.Id)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current employee ID format settings.",
            });
        }

        var deleted = await _formats.DeleteAsync(tenantId, orgId, ct);
        if (!deleted)
            return Respons<object>.Fail("Employee ID format settings not found.", statusCode: 404);

        return Respons<object>.Ok(new { }, detail: "Employee ID format settings deleted.");
    }

    private async Task<EmployeeIdFormatReadDto> MapToReadDtoAsync(
        EmployeeIdFormatEntity entity,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct)
    {
        var users = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(new[] { new[] { entity.CreatedBy, entity.UpdatedBy } }),
            tenantId, ct);

        var preview = await _codeGen.PreviewNextCodeAsync(entity, tenantId, ct);

        return new EmployeeIdFormatReadDto
        {
            Id = entity.Id.ToString(),
            Prefix = entity.Prefix,
            DigitCount = entity.DigitCount,
            StartingNumber = entity.StartingNumber,
            Separator = entity.Separator,
            AutoGenerate = entity.AutoGenerate,
            NextIdPreview = preview,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedById = entity.CreatedBy,
            UpdatedById = entity.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(entity.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(entity.UpdatedBy, users),
        };
    }

    private static Dictionary<string, string>? ValidateFields(
        string? prefix,
        int? digitCount,
        int? startingNumber,
        string? separator,
        bool? autoGenerate)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(prefix))
            errors["prefix"] = "Prefix is required.";
        else if (prefix.Trim().Length > EmployeeIdFormatRules.MaxPrefixLength)
            errors["prefix"] = $"Prefix must be at most {EmployeeIdFormatRules.MaxPrefixLength} characters.";

        if (digitCount is null)
            errors["digit_count"] = "Number of digits is required.";
        else if (digitCount is < EmployeeIdFormatRules.MinDigitCount or > EmployeeIdFormatRules.MaxDigitCount)
            errors["digit_count"] =
                $"Number of digits must be between {EmployeeIdFormatRules.MinDigitCount} and {EmployeeIdFormatRules.MaxDigitCount}.";

        if (startingNumber is null)
            errors["starting_number"] = "Starting number is required.";
        else if (startingNumber < 1)
            errors["starting_number"] = "Starting number must be at least 1.";

        if (string.IsNullOrWhiteSpace(separator))
            errors["separator"] = "Separator is required.";
        else if (!EmployeeIdFormatSeparator.Allowed.Contains(separator.Trim()))
            errors["separator"] = "Separator must be one of: hyphen, none, underscore, slash.";

        if (autoGenerate is null)
            errors["auto_generate"] = "Auto generate flag is required.";

        if (errors.Count == 0
            && prefix is not null
            && digitCount is not null
            && startingNumber is not null
            && separator is not null
            && autoGenerate is not null)
        {
            var probe = new EmployeeIdFormatEntity
            {
                Prefix = prefix.Trim(),
                DigitCount = digitCount.Value,
                StartingNumber = startingNumber.Value,
                Separator = separator.Trim().ToLowerInvariant(),
                AutoGenerate = autoGenerate.Value,
            };
            if (!EmployeeIdFormatRules.FitsMaxLength(probe))
            {
                errors["prefix"] =
                    $"Combined employee ID exceeds {EmployeeIdFormatRules.MaxEmployeeCodeLength} characters — shorten prefix or reduce digit count.";
            }
        }

        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string>? ValidateId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new Dictionary<string, string> { ["id"] = "Employee ID format settings id is required." };
        if (!Guid.TryParse(id, out _))
            return new Dictionary<string, string> { ["id"] = "Employee ID format settings id must be a valid UUID." };
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
}
