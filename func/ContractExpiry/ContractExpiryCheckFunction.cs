using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;

namespace ZelosHR.Functions.ContractExpiry;

/// <summary>
/// Story 1.8 — daily contract expiry checks (placeholder for Sprint 1+).
/// </summary>
public class ContractExpiryCheckFunction
{
    private readonly ILogger<ContractExpiryCheckFunction> _logger;

    public ContractExpiryCheckFunction(ILogger<ContractExpiryCheckFunction> logger) =>
        _logger = logger;

    [Function("ContractExpiryCheck")]
    public Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo timerInfo, CancellationToken ct)
    {
        _logger.LogInformation("Contract expiry check scheduled at {Time}", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
