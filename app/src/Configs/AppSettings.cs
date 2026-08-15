namespace ZelosHR.Api.Configs;

public class AppSettings
{
    public const string SectionName = "App";

    /// <summary>
    /// PostgreSQL URI, e.g. postgresql://user:password@host:5432/dbname?sslmode=require
    /// </summary>
    public string? ConnectionString { get; set; }

    public bool Debug { get; set; }
    public string AppName { get; set; } = "ZelosHR API";
    public string AppVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";

    public string LogLevel { get; set; } = "Information";
    public string LogDir { get; set; } = "logs";

    public string? SecretKey { get; set; }
    public string? Algorithm { get; set; }
    public int AccessTokenExpireMinutes { get; set; } = 300;

    public string CorsOrigins { get; set; } =
        "http://localhost:3000,http://localhost:3003,http://localhost:8080";

    /// <summary>
    /// Used when <see cref="CorsOrigins"/> is empty (e.g. Production json placeholder before env override).
    /// </summary>
    public static readonly string[] LocalDevCorsFallback =
    [
        "http://localhost:3000",
        "http://localhost:3003",
        "http://localhost:8080",
    ];
    public string AppUrl { get; set; } = "https://zeloshr.com";
    public string AppId { get; set; } = "app-zeloshr";

    /// <summary>Employee portal apex domain (e.g. <c>dev.zeloshr.com</c> or <c>zeloshr.com</c>).</summary>
    public string EmployeePortalDomain { get; set; } = "zeloshr.com";

    /// <summary>Activation link TTL in days (email copy and token validation).</summary>
    public int EmployeeActivationExpiryDays { get; set; } = 7;

    /// <summary>Password reset link TTL in hours (email copy and token validation).</summary>
    public int EmployeePasswordResetExpiryHours { get; set; } = 1;

    /// <summary>Max password reset emails per employee within the rate-limit window.</summary>
    public int EmployeePasswordResetRateLimitMax { get; set; } = 5;

    /// <summary>Rate-limit window in minutes for password reset requests.</summary>
    public int EmployeePasswordResetRateLimitWindowMinutes { get; set; } = 60;

    /// <summary>
    /// When false (default), schema is owned by tvs-sqlscript; API does not run embedded SQL on startup.
    /// </summary>
    public bool RunDatabaseMigrations { get; set; }

    // Configurable table names. In deployed environments these are overridden by
    // App__<Name> env vars, sourced centrally from tvs-iac common.hcl `table_names`
    // (the single source of truth, like the other apps). The values below are the
    // local-dev / fallback defaults — not duplicated in appsettings.json anymore.
    // Core platform tables (core_platform schema)
    public string CorePlatformUsersTable { get; set; } = "core_platform.cp_users";
    public string CorePlatformMembersTable { get; set; } = "core_platform.cp_members";

    // ZelosHR application tables (zeloshr schema)
    public string ActivityLogsTable { get; set; } = "zeloshr.zhr_activity_logs";
    public string EmployeesTable { get; set; } = "zeloshr.zhr_employees";

    public IReadOnlyList<string> CorsOriginsList =>
        CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
