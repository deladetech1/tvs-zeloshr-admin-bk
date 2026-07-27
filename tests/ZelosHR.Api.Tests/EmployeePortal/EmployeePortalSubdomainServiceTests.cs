using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.EmployeePortal;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Tests.EmployeePortal;

public class EmployeePortalSubdomainServiceTests
{
    private static EmployeePortalSubdomainEntity DefaultEntity() => new()
    {
        Id = Guid.Parse("d1000002-0000-4000-8000-000000000002"),
        TenantId = "t1",
        OrgId = "o1",
        BusId = "b1",
        LocId = "l1",
        Subdomain = "btl",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static EmployeePortalSubdomainService CreateService(
        IEmployeePortalSubdomainRepository? subdomains = null)
    {
        var repo = subdomains ?? Substitute.For<IEmployeePortalSubdomainRepository>();
        if (subdomains is null)
        {
            repo.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
                .Returns(DefaultEntity());
            repo.GetBySubdomainAsync("btl", Arg.Any<CancellationToken>())
                .Returns(DefaultEntity());
        }

        var appSettings = Options.Create(new AppSettings { AppId = "app-zeloshr" });
        return new EmployeePortalSubdomainService(repo, Substitute.For<ICpUserRepository>(), appSettings);
    }

    [Fact]
    public async Task ListAsync_returns_empty_when_not_configured()
    {
        var repo = Substitute.For<IEmployeePortalSubdomainRepository>();
        repo.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns((EmployeePortalSubdomainEntity?)null);

        var service = CreateService(repo);
        var result = await service.ListAsync("t1", "o1", "user-1");

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_returns_404_when_not_configured()
    {
        var repo = Substitute.For<IEmployeePortalSubdomainRepository>();
        repo.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns((EmployeePortalSubdomainEntity?)null);

        var service = CreateService(repo);
        var result = await service.GetAsync("t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_org()
    {
        var repo = Substitute.For<IEmployeePortalSubdomainRepository>();
        repo.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>()).Returns(DefaultEntity());

        var service = CreateService(repo);
        var result = await service.CreateAsync(
            new CreateEmployeePortalSubdomainDto { Subdomain = "newco" },
            "t1", "o1", "b1", "l1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateAsync_rejects_taken_subdomain()
    {
        var repo = Substitute.For<IEmployeePortalSubdomainRepository>();
        repo.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns((EmployeePortalSubdomainEntity?)null);
        repo.SubdomainTakenAsync("newco", null, Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(repo);
        var result = await service.CreateAsync(
            new CreateEmployeePortalSubdomainDto { Subdomain = "newco" },
            "t1", "o1", "b1", "l1", "user-1");

        result.Success.Should().BeFalse();
        result.FieldErrors.Should().ContainKey("subdomain");
    }

    [Fact]
    public async Task ResolveBySubdomainAsync_returns_context()
    {
        var service = CreateService();
        var result = await service.ResolveBySubdomainAsync("btl");

        result.Success.Should().BeTrue();
        result.Data!.Subdomain.Should().Be("btl");
        result.Data.PortalUrl.Should().Be("btl.zeloshr.com");
        result.Data.AppId.Should().Be("app-zeloshr");
        result.Data.TenantId.Should().Be("t1");
    }

    [Fact]
    public async Task ResolveBySubdomainAsync_returns_404_when_missing()
    {
        var repo = Substitute.For<IEmployeePortalSubdomainRepository>();
        repo.GetBySubdomainAsync("missing", Arg.Any<CancellationToken>())
            .Returns((EmployeePortalSubdomainEntity?)null);

        var service = CreateService(repo);
        var result = await service.ResolveBySubdomainAsync("missing");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
