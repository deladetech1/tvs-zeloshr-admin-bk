using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Configs;

public static class ApiBehaviorConfiguration
{
    public static void ConfigureInvalidModelStateResponse(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = ValidationErrors.FromModelState(context.ModelState);
            var response = Respons<object>.ValidationError(fieldErrors);
            return new BadRequestObjectResult(response);
        };
    }
}
