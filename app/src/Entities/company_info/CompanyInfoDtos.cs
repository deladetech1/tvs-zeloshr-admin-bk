using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Entities.CompanyInfo;

public sealed record CompanyOfficeReadDto
{
    public required string OfficeId { get; init; }
    public required string Name { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? Phone { get; init; }
    public bool IsHeadOffice { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// Entry inside the <c>offices</c> full-replacement array on <c>POST /add</c> / <c>PUT /update</c>.
/// <c>OfficeId</c> absent/null creates a new office; a value matching an existing office for the
/// org replaces its fields; a value matching no office for the org is a validation error.
/// </summary>
public sealed class CompanyOfficeWriteDto
{
    public Guid? OfficeId { get; set; }
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public bool? IsHeadOffice { get; set; }
}

public sealed record CompanyInfoReadDto
{
    public required string Id { get; init; }
    public string? LegalName { get; init; }
    public string? TradingName { get; init; }
    public string? Industry { get; init; }
    public string? CompanySize { get; init; }
    public string? BusinessRegistrationNumber { get; init; }
    public string? Tin { get; init; }
    public string? PrimaryWorkCountry { get; init; }
    public string? CompanyEmail { get; init; }
    public string? Website { get; init; }
    public DocumentReadDto? LogoUrl { get; init; }
    public DocumentReadDto? BannerUrl { get; init; }
    public IReadOnlyList<CompanyOfficeReadDto> Offices { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CreateCompanyInfoDto
{
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? Tin { get; set; }
    public string? PrimaryWorkCountry { get; set; }

    [EmailAddress]
    public string? CompanyEmail { get; set; }
    public string? Website { get; set; }

    /// <summary>
    /// Document registry id from <c>POST /api/v1/file/post/multiple</c> — same as
    /// <c>identity.profile_url</c> on employees. Also accepts the read-shaped object
    /// (<c>doc_id</c> / <c>id</c> / <c>presigned_url</c>) for round-trip from GET.
    /// </summary>
    [JsonConverter(typeof(ProfileUrlWriteJsonConverter))]
    public string? LogoUrl { get; set; }

    [JsonConverter(typeof(ProfileUrlWriteJsonConverter))]
    public string? BannerUrl { get; set; }

    /// <summary>Optional initial offices to create alongside the profile.</summary>
    public List<CompanyOfficeWriteDto>? Offices { get; set; }
}

/// <summary>
/// Same shape as <see cref="CreateCompanyInfoDto"/>, plus <see cref="Id"/> — a full replacement
/// of the profile, not a partial patch. Fields omitted from the body are cleared, just like on
/// create; the only field that's optional-and-preserves-the-existing-value is <see cref="Offices"/>
/// (see "Offices write semantics" in the design spec).
/// </summary>
public sealed class UpdateCompanyInfoDto
{
    /// <summary>Must match the profile's current id (from <c>GET /get</c>).</summary>
    public string? Id { get; set; }
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? Tin { get; set; }
    public string? PrimaryWorkCountry { get; set; }

    [EmailAddress]
    public string? CompanyEmail { get; set; }
    public string? Website { get; set; }

    [JsonConverter(typeof(ProfileUrlWriteJsonConverter))]
    public string? LogoUrl { get; set; }

    [JsonConverter(typeof(ProfileUrlWriteJsonConverter))]
    public string? BannerUrl { get; set; }

    /// <summary>
    /// Full-replacement array — absent leaves offices untouched, <c>[]</c> deletes all,
    /// see "Offices write semantics" in the design spec.
    /// </summary>
    public List<CompanyOfficeWriteDto>? Offices { get; set; }
}
