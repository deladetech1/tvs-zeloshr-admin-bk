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

    /// <summary>
    /// When false (default), schema is owned by tvs-sqlscript; API does not run embedded SQL on startup.
    /// </summary>
    public bool RunDatabaseMigrations { get; set; }

    // Core platform tables (core_platform schema)
    public string CorePlatformUsersTable { get; set; } = "core_platform.cp_users";
    public string CorePlatformMembersTable { get; set; } = "core_platform.cp_members";

    // ZelosHR application tables (zeloshr schema)
    public string ActivityLogsTable { get; set; } = "zeloshr.zhr_activity_logs";
    public string EmployeesTable { get; set; } = "zeloshr.zhr_employees";

    public IReadOnlyList<string> CorsOriginsList =>
        CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
