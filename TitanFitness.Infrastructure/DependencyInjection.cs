using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Infrastructure.Persistence;
using TitanFitness.Infrastructure.Persistence.Repositories;

namespace TitanFitness.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TitanFitnessDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TitanFitness")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<ITrainerRepository, TrainerRepository>();
        services.AddScoped<IClassSessionRepository, ClassSessionRepository>();
        services.AddScoped<ICheckInRepository, CheckInRepository>();

        services.AddScoped<IReadQueries, ReadQueries>();

        return services;
    }
}