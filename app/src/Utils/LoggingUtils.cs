namespace ZelosHR.Api.Utils;

public sealed class LogContext : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _entity;
    private readonly string _action;

    public LogContext(ILogger logger, string entity, string action, string? detail = null)
    {
        _logger = logger;
        _entity = entity;
        _action = action;
        _logger.LogDebug("Starting {Entity}.{Action} {Detail}", entity, action, detail ?? "");
    }

    public void Dispose() =>
        _logger.LogDebug("Completed {Entity}.{Action}", _entity, _action);
}
