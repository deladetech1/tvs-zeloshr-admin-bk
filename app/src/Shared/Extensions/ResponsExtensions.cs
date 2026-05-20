using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Shared.Extensions;

public static class ResponsExtensions
{
    public static IActionResult ToActionResult<T>(this Respons<T> result) =>
        result.StatusCode switch
        {
            StatusCodes.Status200OK => new OkObjectResult(result),
            StatusCodes.Status201Created => new ObjectResult(result) { StatusCode = StatusCodes.Status201Created },
            StatusCodes.Status400BadRequest => new BadRequestObjectResult(result),
            StatusCodes.Status403Forbidden => new ObjectResult(result) { StatusCode = StatusCodes.Status403Forbidden },
            StatusCodes.Status404NotFound => new NotFoundObjectResult(result),
            StatusCodes.Status409Conflict => new ConflictObjectResult(result),
            StatusCodes.Status422UnprocessableEntity => new UnprocessableEntityObjectResult(result),
            _ => new ObjectResult(result) { StatusCode = result.StatusCode },
        };
}
