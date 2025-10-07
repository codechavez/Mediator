using CodeChavez.M3diator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeChavez.M3diator.Register;

/// <summary>
/// Module use to register M3diator
/// </summary>
public static class M3diatorModule
{
    /// <summary>
    /// Register All M3diator IRequestHandler, INotificationHandler, IM3diator
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddM3diator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IM3diator, M3diator>();

        if (assemblies == null || assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableToAny(
                typeof(IRequestHandler<>),
                typeof(IRequestHandler<,>),
                typeof(INotificationHandler<>)
            ))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }
}