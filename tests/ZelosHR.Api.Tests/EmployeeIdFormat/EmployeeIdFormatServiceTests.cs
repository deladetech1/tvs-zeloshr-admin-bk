using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Tests.EmployeeIdFormat;

public class EmployeeIdFormatServiceTests
{
    private static EmployeeIdFormatEntity DefaultEntity() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "t1",
        OrgId = "o1",
        Prefix = "ZEL",
        DigitCount = 4,
        StartingNumber = 1,
        Separator = EmployeeIdFormatSeparator.Hyphen,
        AutoGenerate = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static EmployeeIdFormatService CreateService(
        IEmployeeIdFormatRepository? formats = null,
        IEmployeeRepository? employees = null)
    {
        var formatRepo = formats ?? Substitute.For<IEmployeeIdFormatRepository>();
        var employeeRepo = employees ?? Substitute.For<IEmployeeRepository>();

        if (formats is null)
        {
            formatRepo.EnsureStubAsync("t1", "o1", "user-1", Arg.Any<CancellationToken>())
                .Returns(DefaultEntity());
        }

        employeeRepo.ListEmployeeCodesAsync("t1", Arg.Any<CancellationToken>())
            .Returns(["ZEL-0102"]);

        var codeGen = new EmployeeCodeGenerationService(formatRepo, employeeRepo);
        return new EmployeeIdFormatService(formatRepo, Substitute.For<ICpUserRepository>(), codeGen);
    }

    [Fact]
    public async Task GetAsync_returns_settings_with_next_id_preview()
    {
        var service = CreateService();
        var result = await service.GetAsync("t1", "o1", "user-1");

        result.Success.Should().BeTrue();
        result.Data!.Prefix.Should().Be("ZEL");
        result.Data.NextIdPreview.Should().Be("ZEL-0103");
    }

    [Fact]
    public async Task UpdateAsync_rejects_invalid_separator()
    {
        var existing = DefaultEntity();
        var formats = Substitute.For<IEmployeeIdFormatRepository>();
        formats.GetEntityAsync("t1", "o1", Arg.Any<CancellationToken>()).Returns(existing);

        var service = CreateService(formats: formats);
        var result = await service.UpdateAsync(
            new UpdateEmployeeIdFormatDto
            {
                Id = existing.Id.ToString(),
                Prefix = "ZEL",
                DigitCount = 4,
                StartingNumber = 1,
                Separator = "pipe",
                AutoGenerate = true,
            },
            "t1", "o1", "user-1");

        result.Success.Should().BeFalse();
        result.FieldErrors!["separator"].Should().Contain("hyphen");
    }
}
