using Trovesuite.Package.Configuration;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Database;
using ZelosHR.Api.Middleware;
using ZelosHR.Api.Shared.Tenant;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddZelosHrStorage(builder.Configuration);
builder.Services.AddEntityServices();
builder.Services.AddScoped<IEmployeesService>(sp => sp.GetRequiredService<EmployeesService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddZelosHrSwagger();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? ["http://localhost:3000"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<TroveRequestHeadersMiddleware>();
app.UseMiddleware<TrovesuiteAuthMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseCors();

app.UseZelosHrSwagger();
app.MapZelosHrSwaggerDevBootstrap();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
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
