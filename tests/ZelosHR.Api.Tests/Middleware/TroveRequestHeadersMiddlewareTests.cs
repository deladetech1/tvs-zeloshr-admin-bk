using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Middleware;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Tests.Middleware;

public class TroveRequestHeadersMiddlewareTests
{
    private static IPlatformContextRepository ValidPlatform() =>
        Substitute.For<IPlatformContextRepository>();

    private static TroveRequestHeadersMiddleware CreateSut(RequestDelegate next) =>
        new(next, NullLogger<TroveRequestHeadersMiddleware>.Instance);

    private static (TroveRequestHeadersMiddleware Sut, IPlatformContextRepository Platform) CreateWithPlatform(
        Action? onNext = null)
    {
        var platform = ValidPlatform();
        platform.ValidateSessionContextAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut(_ =>
        {
            onNext?.Invoke();
            return Task.CompletedTask;
        });
        return (sut, platform);
    }

    private static DefaultHttpContext CreateContext(string path = "/api/v1/employees")
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        ctx.Request.Path = path;
        return ctx;
    }

    private static IOptions<TrovesuiteIntegrationOptions> IntegrationOptions(
        bool requireHeaders = true,
        bool requireAuth = false,
        bool validatePlatform = true) =>
        Options.Create(new TrovesuiteIntegrationOptions
        {
            RequireStandardHeaders = requireHeaders,
            RequireAuthentication = requireAuth,
            ValidatePlatformContext = validatePlatform,
        });

    private static IOptions<AppSettings> AppSettingsOptions() =>
        Options.Create(new AppSettings());

    [Fact]
    public async Task Invoke_WhenRequiredHeadersMissing_Returns400()
    {
        var ctx = CreateContext();
        var sut = CreateSut(_ => Task.CompletedTask);
        var platform = ValidPlatform();

        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Invoke_WithAllStandardHeaders_PassesThrough()
    {
        var ctx = CreateContext();
        ctx.Request.Headers[TroveStandardHeaders.AppId] = TroveStandardHeaders.HrAppId;
        ctx.Request.Headers[TroveStandardHeaders.Authorization] =
            "Bearer eyJhbGciOiJIUzI1NiJ9.eyJ1c2VyX2lkIjoidTEiLCJ0ZW5hbnRfaWQiOiJkZW1vLXRlbmFudCJ9.sig";
        ctx.Request.Headers[TroveStandardHeaders.BusId] = TestDefaults.BusId;
        ctx.Request.Headers[TroveStandardHeaders.LocId] = TestDefaults.LocId;
        ctx.Request.Headers[TroveStandardHeaders.OrgId] = TestDefaults.OrgId;

        var nextCalled = false;
        var (sut, platform) = CreateWithPlatform(() => nextCalled = true);
        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        nextCalled.Should().BeTrue();
        ctx.Items[TrovesuiteHttpContextKeys.OrgId].Should().Be(TestDefaults.OrgId);
        ctx.Items[TrovesuiteHttpContextKeys.BusId].Should().Be(TestDefaults.BusId);
    }

    [Fact]
    public async Task Invoke_WhenAppIdIsNotHr_Returns400()
    {
        var ctx = CreateContext();
        ctx.Request.Headers[TroveStandardHeaders.AppId] = "app-other";
        ctx.Request.Headers[TroveStandardHeaders.Authorization] = "Bearer eyJhbGciOiJIUzI1NiJ9.e30.sig";
        ctx.Request.Headers[TroveStandardHeaders.BusId] = TestDefaults.BusId;
        ctx.Request.Headers[TroveStandardHeaders.LocId] = TestDefaults.LocId;
        ctx.Request.Headers[TroveStandardHeaders.OrgId] = TestDefaults.OrgId;

        var (sut, platform) = CreateWithPlatform();
        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Invoke_WhenPlatformContextInvalid_Returns403()
    {
        var ctx = CreateContext();
        ctx.Request.Headers[TroveStandardHeaders.AppId] = TroveStandardHeaders.HrAppId;
        ctx.Request.Headers[TroveStandardHeaders.Authorization] =
            "Bearer eyJhbGciOiJIUzI1NiJ9.eyJ1c2VyX2lkIjoidTEiLCJ0ZW5hbnRfaWQiOiJkZW1vLXRlbmFudCJ9.sig";
        ctx.Request.Headers[TroveStandardHeaders.BusId] = TestDefaults.BusId;
        ctx.Request.Headers[TroveStandardHeaders.LocId] = TestDefaults.LocId;
        ctx.Request.Headers[TroveStandardHeaders.OrgId] = TestDefaults.OrgId;

        var platform = ValidPlatform();
        platform.ValidateSessionContextAsync(
                TestDefaults.TenantId, "u1", TestDefaults.OrgId, TestDefaults.BusId, TestDefaults.LocId,
                TestDefaults.AppId, Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut(_ => Task.CompletedTask);
        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task ActivationPath_SkipsHeaderEnforcement()
    {
        var ctx = CreateContext("/api/v1/employee-portal/activation/validate");
        var nextCalled = false;
        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var platform = ValidPlatform();

        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task PasswordResetPath_SkipsHeaderEnforcement()
    {
        var ctx = CreateContext("/api/v1/employee-portal/password-reset/validate");
        var nextCalled = false;
        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var platform = ValidPlatform();

        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HealthPath_SkipsHeaderEnforcement()
    {
        var ctx = CreateContext("/api/v1/health");
        var nextCalled = false;
        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var platform = ValidPlatform();

        await sut.InvokeAsync(ctx, IntegrationOptions(), AppSettingsOptions(), platform);

        nextCalled.Should().BeTrue();
    }
}
