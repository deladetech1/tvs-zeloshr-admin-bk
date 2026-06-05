using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Shared.Validation;

public static class QueryParamValidation
{
    public static ActionResult? BadRequestIfEmptyGuid<T>(Guid id, string paramName)
    {
        if (id != Guid.Empty)
            return null;

        return new BadRequestObjectResult(
            Respons<T>.ValidationError(ValidationErrors.RequiredQueryParam(paramName)));
    }

    public static ActionResult? BadRequestIfMissing<T>(string? value, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return null;

        return new BadRequestObjectResult(
            Respons<T>.ValidationError(ValidationErrors.RequiredQueryParam(paramName)));
    }
}
