using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Shared;

/// <summary>Liveness and readiness probes (no tenant headers).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Health, IgnoreApi = true)]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IDatabaseManager _database;

    public HealthController(IDatabaseManager database) => _database = database;

    [HttpGet]
    public async Task<IActionResult> HealthCheck(CancellationToken ct)
    {
        var db = await _database.HealthCheckAsync(ct);
        return Ok(new
        {
            status = "healthy",
            database = db,
            build = Environment.GetEnvironmentVariable("BUILD_VERSION")
                ?? typeof(HealthController).Assembly.GetName().Version?.ToString(),
        });
    }

    [HttpGet("live")]
    public IActionResult Liveness() => Ok(new { status = "live" });

    [HttpGet("ready")]
    public async Task<IActionResult> Readiness(CancellationToken ct)
    {
        var db = await _database.HealthCheckAsync(ct);
        if (db.Status == "healthy")
            return Ok(new { status = "ready" });

        return StatusCode(503, new { status = "not ready", database = db });
    }
}
