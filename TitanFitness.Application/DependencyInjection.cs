using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TitanFitness.Application.Common;

namespace TitanFitness.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        // Request validators live next to their request in each feature's Contracts folder.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<Features.ClassSessions.Shared.ClassSessionChecks>();

        return services;
    }
}
