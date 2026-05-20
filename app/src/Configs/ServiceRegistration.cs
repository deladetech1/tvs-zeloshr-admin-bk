using System.Reflection;

namespace ZelosHR.Api.Configs;

public static class ServiceRegistration
{
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
