namespace ZelosHR.Api.Configs;

/// <summary>Swagger UI tag names — keep in sync with <see cref="SwaggerConfiguration"/>.</summary>
public static class SwaggerGroups
{
    /// <summary>Modules documented in Swagger UI (sprint scope). All others use <c>IgnoreApi</c>.</summary>
    public static readonly HashSet<string> VisibleInSwagger = new(StringComparer.Ordinal)
    {
        Employees,
        Currencies,
        CustomFields,
        FileManagement,
        Organisation,
        LifecycleEvents,
        AuditLogs,
    };

    public static bool IsVisibleInSwagger(string? groupName) =>
        groupName is { Length: > 0 } && VisibleInSwagger.Contains(groupName);

    public const string Discovery = "Discovery";
    public const string Health = "Health";
    public const string Dashboard = "Dashboard";
    public const string Employees = "Employees";
    public const string Currencies = "Currencies";
    public const string Organisation = "Organisation";
    public const string OrganisationLegacy = "Organisation (legacy)";
    public const string LifecycleEvents = "Lifecycle Events";
    public const string AuditLogs = "Audit Logs";
    public const string Attendance = "Attendance";
    public const string Leave = "Leave";
    public const string Recruitment = "Recruitment";
    public const string Onboarding = "Onboarding";
    public const string Performance = "Performance";
    public const string Disciplinary = "Disciplinary";
    public const string Documents = "Documents";
    public const string CustomFields = "Custom Fields";
    public const string FileManagement = "File Management";
    public const string TrovesuitePlatform = "Trovesuite Platform";
}
