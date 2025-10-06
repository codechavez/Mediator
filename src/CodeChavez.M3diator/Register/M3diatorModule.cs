using CodeChavez.M3diator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeChavez.M3diator.Register;

public static class M3diatorModule
{
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