using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Tests.CompanyInfo;

public class CompanyInfoServiceTests
{
    private static HrDocumentPresignedUrlService DocumentUrls() =>
        new(
            Substitute.For<IEmployeeDocumentBlobStorage>(),
            Substitute.For<IHrDocumentPathRepository>(),
            new FileManagementStorage(Substitute.For<IConfiguration>(), Options.Create(new AzureStorageOptions())),
            Substitute.For<ITenantContext>());

    private static CompanyInfoService CreateService(
        ICompanyProfileRepository? profiles = null,
        ICompanyOfficeRepository? offices = null) =>
        new(
            profiles ?? Substitute.For<ICompanyProfileRepository>(),
            offices ?? Substitute.For<ICompanyOfficeRepository>(),
            Substitute.For<ICpUserRepository>(),
            DocumentUrls(),
            db: null!);

    [Fact]
    public async Task CreateAsync_rejects_missing_legal_name()
    {
        var service = CreateService();
        var result = await service.CreateAsync(
            new CreateCompanyInfoDto { LegalName = "  " }, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["legal_name"].Should().Contain("required");
    }

    [Fact]
    public async Task CreateAsync_rejects_when_profile_already_exists()
    {
        var profiles = Substitute.For<ICompanyProfileRepository>();
        profiles.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyProfileEntity { Id = Guid.NewGuid(), TenantId = "t1", OrgId = "o1", LegalName = "Marvel" });

        var service = CreateService(profiles: profiles);
        var result = await service.CreateAsync(
            new CreateCompanyInfoDto { LegalName = "Marvel Industries" }, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["legal_name"].Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_office_names()
    {
        var service = CreateService();
        var result = await service.CreateAsync(
            new CreateCompanyInfoDto
            {
                LegalName = "Marvel Industries",
                Offices =
                [
                    new CompanyOfficeWriteDto { Name = "Accra Office" },
                    new CompanyOfficeWriteDto { Name = "accra office" },
                ],
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["offices[1].name"].Should().Contain("Duplicate");
    }

    [Fact]
    public async Task UpdateAsync_rejects_missing_id()
    {
        var service = CreateService();
        var result = await service.UpdateAsync(
            new UpdateCompanyInfoDto { LegalName = "Marvel Industries" }, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["id"].Should().Contain("required");
    }

    [Fact]
    public async Task UpdateAsync_rejects_invalid_id_format()
    {
        var service = CreateService();
        var result = await service.UpdateAsync(
            new UpdateCompanyInfoDto { Id = "not-a-guid", LegalName = "Marvel Industries" }, "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["id"].Should().Contain("UUID");
    }

    [Fact]
    public async Task UpdateAsync_returns_404_when_profile_missing()
    {
        var service = CreateService();
        var result = await service.UpdateAsync(
            new UpdateCompanyInfoDto { Id = Guid.NewGuid().ToString(), LegalName = "Marvel Industries" },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateAsync_rejects_id_that_does_not_match_existing_profile()
    {
        var profiles = Substitute.For<ICompanyProfileRepository>();
        profiles.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyProfileEntity { Id = Guid.NewGuid(), TenantId = "t1", OrgId = "o1", LegalName = "Marvel" });

        var service = CreateService(profiles: profiles);
        var result = await service.UpdateAsync(
            new UpdateCompanyInfoDto { Id = Guid.NewGuid().ToString(), LegalName = "Marvel Industries" },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["id"].Should().Contain("does not match");
    }

    [Fact]
    public async Task UpdateAsync_rejects_unknown_office_id()
    {
        var existingId = Guid.NewGuid();
        var profiles = Substitute.For<ICompanyProfileRepository>();
        profiles.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new CompanyProfileEntity { Id = existingId, TenantId = "t1", OrgId = "o1", LegalName = "Marvel" });

        var unknownId = Guid.NewGuid();
        var officeRepo = Substitute.For<ICompanyOfficeRepository>();
        officeRepo.ReplaceAllAsync("t1", "o1", Arg.Any<IReadOnlyList<CompanyOfficeWriteDto>>(), "user-1", Arg.Any<CancellationToken>())
            .Returns((new List<CompanyOfficeEntity>(), new List<Guid> { unknownId }));

        var service = CreateService(profiles: profiles, offices: officeRepo);
        var result = await service.UpdateAsync(
            new UpdateCompanyInfoDto
            {
                Id = existingId.ToString(),
                LegalName = "Marvel Industries",
                Offices = [new CompanyOfficeWriteDto { OfficeId = unknownId, Name = "Accra Office" }],
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["offices"].Should().Contain(unknownId.ToString());
    }
}
