using TitanFitness.Domain.Common;

namespace TitanFitness.Infrastructure.Persistence;

public sealed class UnitOfWork(TitanFitnessDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}