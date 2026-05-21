using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using ZelosHR.Api.Entities.Attendance;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Dashboard;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Disciplinary;
using ZelosHR.Api.Entities.Documents;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Entities.LifecycleEvents;
using ZelosHR.Api.Entities.Onboarding;
using ZelosHR.Api.Entities.Performance;
using ZelosHR.Api.Entities.Recruitment;
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
            options
                .UseNpgsql(BuildConnectionString(settings))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICpUserRepository, CpUserRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ILifecycleEventRepository, LifecycleEventRepository>();
        services.AddScoped<IEmployeeDirectoryRepository, EmployeeDirectoryRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IRecruitmentRepository, RecruitmentRepository>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<IPerformanceRepository, PerformanceRepository>();
        services.AddScoped<IDisciplinaryRepository, DisciplinaryRepository>();
        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
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
