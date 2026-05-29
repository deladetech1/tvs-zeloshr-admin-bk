using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeSubResourcesTests
{
    private readonly IEmployeeEducationRepository _education = Substitute.For<IEmployeeEducationRepository>();
    private readonly IEmployeeCertificationRepository _certifications = Substitute.For<IEmployeeCertificationRepository>();
    private readonly IEmployeeWizardDocumentRepository _documents = Substitute.For<IEmployeeWizardDocumentRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IFileStorageService _files = Substitute.For<IFileStorageService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly EmployeeSubResourcesService _sut;

    public EmployeeSubResourcesTests()
    {
        _tenant.TenantId.Returns(TestDefaults.TenantId);
        _tenant.OrgId.Returns(TestDefaults.OrgId);
        _sut = new EmployeeSubResourcesService(
            _education,
            _certifications,
            _documents,
            _employees,
            _files,
            Options.Create(new AzureStorageOptions()),
            _tenant,
            _currentUser);
    }

    [Fact]
    public async Task UploadDocument_WhenFileExceeds10Mb_ReturnsValidationError()
    {
        var employeeId = Guid.NewGuid();
        _employees.GetByIdScopedAsync(employeeId, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(new EmployeeEntity { Id = employeeId, FullName = "Test" });

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await _sut.UploadDocumentAsync(
            employeeId,
            "contract",
            stream,
            "big.pdf",
            "application/pdf",
            fileSize: 10 * 1024 * 1024 + 1);

        result.Success.Should().BeFalse();
        result.FieldErrors.Should().ContainKey("file");
    }
}
