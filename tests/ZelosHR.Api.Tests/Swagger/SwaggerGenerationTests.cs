using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Entities.CompanyLocalization;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Entities.Currencies;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.IdCardTypes;
using ZelosHR.Api.Entities.Leave;

namespace ZelosHR.Api.Tests.Swagger;

public sealed class SwaggerGenerationTests
{
    [Fact]
    public void Generate_v1_openapi_document_includes_documented_modules()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers()
            .AddApplicationPart(typeof(EmployeesController).Assembly)
            .AddApplicationPart(typeof(CurrenciesController).Assembly)
            .AddApplicationPart(typeof(CustomFieldsController).Assembly)
            .AddApplicationPart(typeof(FileManagementController).Assembly)
            .AddApplicationPart(typeof(LeaveController).Assembly)
            .AddApplicationPart(typeof(IdCardTypesController).Assembly)
            .AddApplicationPart(typeof(CompanyInfoController).Assembly)
            .AddApplicationPart(typeof(CompanyLocalizationController).Assembly)
            .AddApplicationPart(typeof(EmployeeIdFormatController).Assembly);
        services.AddEndpointsApiExplorer();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TestWebHostEnvironment>();
        services.AddSingleton<IWebHostEnvironment>(sp => sp.GetRequiredService<TestWebHostEnvironment>());
        services.AddSingleton<IHostEnvironment>(sp => sp.GetRequiredService<TestWebHostEnvironment>());
        services.AddZelosHrSwagger(configuration);

        using var provider = services.BuildServiceProvider();
        var swaggerProvider = provider.GetRequiredService<ISwaggerProvider>();

        var document = swaggerProvider.GetSwagger("v1");
        document.Should().NotBeNull();
        document.Paths.Should().ContainKey("/api/v1/employees/import");
        document.Paths.Should().ContainKey("/api/v1/file/post/multiple");
        document.Paths.Should().ContainKey("/api/v1/currencies/list");
        document.Paths.Should().ContainKey("/api/v1/leave/dashboard");
        document.Paths.Should().ContainKey("/api/v1/leave/requests/list");
        document.Paths.Should().ContainKey("/api/v1/leave/summary");
        document.Paths.Should().ContainKey("/api/v1/leave/my/summary");
        document.Paths.Should().ContainKey("/api/v1/employment-types/list");
        document.Paths.Should().ContainKey("/api/v1/id-card-types/list");
        document.Paths.Should().ContainKey("/api/v1/id-card-types/get");
        document.Paths.Should().ContainKey("/api/v1/id-card-types/add");
        document.Paths.Should().ContainKey("/api/v1/id-card-types/update");
        document.Paths.Should().ContainKey("/api/v1/id-card-types/delete");
        document.Paths.Should().ContainKey("/api/v1/company/info/get");
        document.Paths.Should().ContainKey("/api/v1/company/info/add");
        document.Paths.Should().ContainKey("/api/v1/company/info/update");
        document.Paths.Should().ContainKey("/api/v1/company/info/delete");
        document.Paths.Should().ContainKey("/api/v1/company/localization/get");
        document.Paths.Should().ContainKey("/api/v1/company/localization/add");
        document.Paths.Should().ContainKey("/api/v1/company/localization/update");
        document.Paths.Should().ContainKey("/api/v1/company/localization/delete");
        document.Paths.Should().ContainKey("/api/v1/company/id-format/list");
        document.Paths.Should().ContainKey("/api/v1/company/id-format/get");
        document.Paths.Should().ContainKey("/api/v1/company/id-format/add");
        document.Paths.Should().ContainKey("/api/v1/company/id-format/update");
        document.Paths.Should().ContainKey("/api/v1/company/id-format/delete");
        document.Paths.Should().ContainKey("/api/v1/company/portal-subdomain/list");
        document.Paths.Should().ContainKey("/api/v1/company/portal-subdomain/get");
        document.Paths.Should().ContainKey("/api/v1/company/portal-subdomain/add");
        document.Paths.Should().ContainKey("/api/v1/company/portal-subdomain/update");
        document.Paths.Should().ContainKey("/api/v1/company/portal-subdomain/delete");
        document.Paths.Should().ContainKey("/api/v1/public/employee-portal/resolve");
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ZelosHR.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    }
}
