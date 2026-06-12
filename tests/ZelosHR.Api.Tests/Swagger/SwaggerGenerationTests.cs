using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Currencies;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
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
            .AddApplicationPart(typeof(LeaveController).Assembly);
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
        document.Paths.Should().ContainKey("/api/v1/leave/my/summary");
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
