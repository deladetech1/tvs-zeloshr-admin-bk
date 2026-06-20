using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Tests.EmploymentTypes;

public class EmploymentTypesServiceTests
{
    [Fact]
    public async Task UpdateAsync_rejects_rename_for_system_default()
    {
        var typeId = Guid.NewGuid();
        var repository = Substitute.For<IEmploymentTypeRepository>();
        repository
            .GetEntityByIdScopedAsync(typeId, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new EmploymentTypeEntity
            {
                Id = typeId,
                TenantId = "t1",
                OrgId = "o1",
                Name = "Full-time",
                IsSystemDefault = true,
                IsActive = true,
            });

        var service = new EmploymentTypesService(repository, Substitute.For<ICpUserRepository>());
        var result = await service.UpdateAsync(
            typeId,
            new UpdateEmploymentTypeDto { Name = "Renamed Full-time" },
            "t1",
            "o1",
            "user-1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["name"].Should().Contain("cannot be renamed");
    }

    [Fact]
    public async Task DeleteAsync_rejects_system_default()
    {
        var typeId = Guid.NewGuid();
        var repository = Substitute.For<IEmploymentTypeRepository>();
        repository
            .GetEntityByIdScopedAsync(typeId, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new EmploymentTypeEntity
            {
                Id = typeId,
                TenantId = "t1",
                OrgId = "o1",
                Name = "Full-time",
                IsSystemDefault = true,
                IsActive = true,
            });

        var service = new EmploymentTypesService(repository, Substitute.For<ICpUserRepository>());
        var result = await service.DeleteAsync(typeId, "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.FieldErrors!["employment_type_id"].Should().Contain("cannot be deleted");
    }
}
