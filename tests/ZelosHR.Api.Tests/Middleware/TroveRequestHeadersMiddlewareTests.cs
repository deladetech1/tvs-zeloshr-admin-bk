using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Middleware;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Tests.Middleware;

public class TroveRequestHeadersMiddlewareTests
{
    private static TroveRequestHeadersMiddleware CreateSut(Action? onNext = null) =>
        new(_ =>
        {
            onNext?.Invoke();
            return Task.CompletedTask;
        });

    private static DefaultHttpContext CreateContext(string path = "/api/v1/employees")
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        ctx.Request.Path = path;
        return ctx;
    }

    private static IOptions<TrovesuiteIntegrationOptions> IntegrationOptions(
        bool requireHeaders = true,
        bool requireAuth = false) =>
        Microsoft.Extensions.Options.Options.Create(new TrovesuiteIntegrationOptions
        {
            RequireStandardHeaders = requireHeaders,
            RequireAuthentication = requireAuth,
        });

    [Fact]
    public async Task Invoke_WhenRequiredHeadersMissing_Returns400()
    {
        var ctx = CreateContext();
        var sut = CreateSut();

        await sut.InvokeAsync(ctx, IntegrationOptions());

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Invoke_WithAllStandardHeaders_PassesThrough()
    {
        var ctx = CreateContext();
        ctx.Request.Headers[TroveStandardHeaders.AppId] = TroveStandardHeaders.HrAppId;
        ctx.Request.Headers[TroveStandardHeaders.Authorization] = "Bearer eyJhbGciOiJIUzI1NiJ9.e30.sig";
        ctx.Request.Headers[TroveStandardHeaders.BusId] = "bus_demo";
        ctx.Request.Headers[TroveStandardHeaders.LocId] = "loc_demo";
        ctx.Request.Headers[TroveStandardHeaders.OrgId] = "org_demo";

        var nextCalled = false;
        var sut = CreateSut(() => nextCalled = true);
        await sut.InvokeAsync(ctx, IntegrationOptions());

        nextCalled.Should().BeTrue();
        ctx.Items[TrovesuiteHttpContextKeys.OrgId].Should().Be("org_demo");
        ctx.Items[TrovesuiteHttpContextKeys.BusId].Should().Be("bus_demo");
    }

    [Fact]
    public async Task Invoke_WhenAppIdIsNotHr_Returns400()
    {
        var ctx = CreateContext();
        ctx.Request.Headers[TroveStandardHeaders.AppId] = "app-other";
        ctx.Request.Headers[TroveStandardHeaders.Authorization] = "Bearer eyJhbGciOiJIUzI1NiJ9.e30.sig";
        ctx.Request.Headers[TroveStandardHeaders.BusId] = "bus_demo";
        ctx.Request.Headers[TroveStandardHeaders.LocId] = "loc_demo";
        ctx.Request.Headers[TroveStandardHeaders.OrgId] = "org_demo";

        var sut = CreateSut();
        await sut.InvokeAsync(ctx, IntegrationOptions());

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task HealthPath_SkipsHeaderEnforcement()
    {
        var ctx = CreateContext("/api/v1/health");
        var nextCalled = false;
        var sut = CreateSut(() => nextCalled = true);

        await sut.InvokeAsync(ctx, IntegrationOptions());

        nextCalled.Should().BeTrue();
    }
}
