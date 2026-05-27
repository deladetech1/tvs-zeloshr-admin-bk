using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Shared;

public abstract class PlatformCrudControllerBase : ControllerBase
{
    protected static ActionResult<Respons<T>> MissingId<T>(string queryName) =>
        BadRequest(Respons<T>.ValidationError(
            new Dictionary<string, string> { [queryName] = "Required query parameter is missing." }));
}
