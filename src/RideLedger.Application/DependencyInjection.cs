using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace RideLedger.Application;

/// <summary>
/// APPLICATION LAYER - Dependency Injection
/// Extension methods for registering application services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Application Services
        // Services will be registered here as they're implemented

        return services;
    }
}
