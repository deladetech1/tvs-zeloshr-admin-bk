using Trovesuite.Package.Configuration;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Database;
using ZelosHR.Api.Middleware;
using ZelosHR.Api.Shared.Tenant;

var builder = WebApplication.CreateBuilder(args);

AppConnectionString.ApplyPackageDatabaseConfiguration(builder.Configuration);
JwtSecretConfiguration.Apply(builder.Configuration);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<TrovesuiteIntegrationOptions>(
    builder.Configuration.GetSection(TrovesuiteIntegrationOptions.SectionName));
builder.Services.Configure<LocalDevelopmentOptions>(
    builder.Configuration.GetSection(LocalDevelopmentOptions.SectionName));

// TroveSuite shared package: auth, notifications, Azure storage (core_platform schema)
builder.Services.AddTrovesuite(builder.Configuration);

builder.Services.AddSingleton<IDatabaseManager, DatabaseManager>();
builder.Services.AddScoped<ISchemaInitializer, SchemaInitializer>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddSharedInfrastructure();
builder.Services.AddZelosHrPersistence(builder.Configuration);
builder.Services.AddZelosHrStorage(builder.Configuration, builder.Environment);
builder.Services.AddEntityServices();
builder.Services.AddScoped<IEmployeesService>(sp => sp.GetRequiredService<EmployeesService>());
builder.Services.AddScoped<IEmployeeLookup>(sp => sp.GetRequiredService<EmployeesService>());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = PlatformJson.SerializerOptions.PropertyNamingPolicy;
        options.JsonSerializerOptions.DictionaryKeyPolicy = PlatformJson.SerializerOptions.DictionaryKeyPolicy;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive =
            PlatformJson.SerializerOptions.PropertyNameCaseInsensitive;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            PlatformJson.SerializerOptions.DefaultIgnoreCondition;
    });
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = PlatformJson.SerializerOptions.PropertyNamingPolicy;
    options.SerializerOptions.DictionaryKeyPolicy = PlatformJson.SerializerOptions.DictionaryKeyPolicy;
    options.SerializerOptions.PropertyNameCaseInsensitive =
        PlatformJson.SerializerOptions.PropertyNameCaseInsensitive;
    options.SerializerOptions.DefaultIgnoreCondition =
        PlatformJson.SerializerOptions.DefaultIgnoreCondition;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddZelosHrSwagger(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var appSettings = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();
        var origins = (appSettings.CorsOriginsList.Count > 0
            ? appSettings.CorsOriginsList
            : AppSettings.LocalDevCorsFallback).ToArray();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

// CORS first; exception handler wraps Trove/auth/controllers so failures return JSON
// (and browsers still see Access-Control-Allow-Origin on error responses).
app.UseCors();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<TroveRequestHeadersMiddleware>();
app.UseMiddleware<TrovesuiteAuthMiddleware>();

app.MapControllers();
app.UseZelosHrSwagger();
app.MapZelosHrSwaggerDevBootstrap();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var integrationOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<TrovesuiteIntegrationOptions>>().Value;
if (integrationOptions.RequireAuthentication
    && !JwtSecretConfiguration.IsConfigured(app.Configuration))
{
    logger.LogCritical(
        "JWT signing key is missing. {Message}",
        JwtSecretConfiguration.MissingKeyMessage);
}

try
{
    var db = app.Services.GetRequiredService<IDatabaseManager>();
    await db.InitializeAsync();
    logger.LogInformation("Database connection pool initialized");

    var appSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;
    if (appSettings.RunDatabaseMigrations)
    {
        using var scope = app.Services.CreateScope();
        var schema = scope.ServiceProvider.GetRequiredService<ISchemaInitializer>();
        await schema.InitializeAsync();
        logger.LogWarning(
            "Applied embedded SQL migrations from ZelosHR.Api (dev only). Production schema must come from tvs-sqlscript.");
    }
    else
    {
        logger.LogInformation(
            "Skipping embedded SQL migrations. Deploy schema via ../tvs-sqlscript (see AGENTS.md).");
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Database initialization failed");
    throw;
}

logger.LogInformation("ZelosHR application startup completed");
app.Run();
