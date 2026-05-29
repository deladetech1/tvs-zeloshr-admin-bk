namespace ZelosHR.Api.Entities.Employees;

/// <summary>Suggested values for <c>identity.id_type</c> (free text; not enforced).</summary>
public static class EmployeeIdTypes
{
    public const string GhanaCard = "ghana_card";
    public const string Passport = "passport";
    public const string VoterId = "voter_id";
    public const string DriversLicense = "drivers_license";
    public const string Ssnit = "ssnit";
    public const string Other = "other";

    public static IReadOnlyList<string> Suggested { get; } =
    [
        GhanaCard,
        Passport,
        VoterId,
        DriversLicense,
        Ssnit,
        Other,
    ];
}
