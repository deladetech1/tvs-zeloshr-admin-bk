using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using ZelosHR.Api.Entities.Attendance;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Dashboard;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Disciplinary;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Documents;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Entities.LifecycleEvents;
using ZelosHR.Api.Entities.Onboarding;
using ZelosHR.Api.Entities.Performance;
using ZelosHR.Api.Entities.Recruitment;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Entities.OrgStructure;
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
                .UseNpgsql(AppConnectionString.Build(settings))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IEmploymentTypeRepository, EmploymentTypeRepository>();
        services.AddScoped<IIdCardTypeRepository, IdCardTypeRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICpUserRepository, CpUserRepository>();
        services.AddScoped<ICpCurrencyRepository, CpCurrencyRepository>();
        services.AddScoped<IPlatformContextRepository, PlatformContextRepository>();
        services.AddScoped<IEmployeeEducationRepository, EmployeeEducationRepository>();
        services.AddScoped<IEmployeeCertificationRepository, EmployeeCertificationRepository>();
        services.AddScoped<IEmployeeWizardDocumentRepository, EmployeeWizardDocumentRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IOrgChartRepository, OrgChartRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<ILifecycleEventRepository, LifecycleEventRepository>();
        services.AddScoped<IEmployeeDirectoryRepository, EmployeeDirectoryRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IRecruitmentRepository, RecruitmentRepository>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<IPerformanceRepository, PerformanceRepository>();
        services.AddScoped<IDisciplinaryRepository, DisciplinaryRepository>();
        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<ICustomFieldDefinitionsRepository, CustomFieldDefinitionsRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IHrDocumentPathRepository, HrDocumentPathRepository>();
        services.AddScoped<FileManagementStorage>();
        return services;
    }
}
