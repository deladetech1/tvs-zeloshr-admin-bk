namespace ZelosHR.Api.Persistence.Repositories;

public interface ICpBusinessRepository
{
    Task<string?> GetBusNameAsync(string tenantId, string busId, CancellationToken ct = default);
}
