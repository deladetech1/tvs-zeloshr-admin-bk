using System.Reflection;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Configs;

public static class ServiceRegistration
{
  public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
  {
    services.AddScoped<ITenantContext, TenantContextAdapter>();
    services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
    return services;
  }

  /// <summary>
  /// Registers all *Service classes from the assembly as scoped services.
  /// </summary>
  public static IServiceCollection AddEntityServices(this IServiceCollection services)
  {
    var assembly = Assembly.GetExecutingAssembly();
    var serviceTypes = assembly.GetTypes()
      .Where(t => t is { IsClass: true, IsAbstract: false }
                  && t.Name.EndsWith("Service", StringComparison.Ordinal)
                  && t.Namespace?.Contains(".Entities.", StringComparison.Ordinal) == true);

    foreach (var type in serviceTypes)
      services.AddScoped(type);

    return services;
  }
}
