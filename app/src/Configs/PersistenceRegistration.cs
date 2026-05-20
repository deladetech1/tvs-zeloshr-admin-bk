using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Configs;

public static class PersistenceRegistration
{
    public static IServiceCollection AddZelosHrPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ZelosHrDbContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
            options.UseNpgsql(BuildConnectionString(settings));
        });

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        return services;
    }

    internal static string BuildConnectionString(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.DatabaseUrl))
            return settings.DatabaseUrl;

        return new NpgsqlConnectionStringBuilder
        {
            Host = settings.DbHost ?? "localhost",
            Port = int.TryParse(settings.DbPort, out var port) ? port : 5431,
            Database = settings.DbName ?? "zeloshrdb",
            Username = settings.DbUser ?? "user",
            Password = settings.DbPassword ?? "password",
        }.ConnectionString;
    }
}
