using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Infrastructure.Persistence;
using TitanFitness.Infrastructure.Persistence.Repositories;
using TitanFitness.Infrastructure.Persistence.Seeding;
using TitanFitness.Infrastructure.Services;

namespace TitanFitness.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TitanFitnessDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TitanFitness")));

        // The only two repositories: generic read and generic write.
        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
