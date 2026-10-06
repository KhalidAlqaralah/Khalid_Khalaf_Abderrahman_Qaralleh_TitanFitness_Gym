using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Infrastructure.Persistence.Repositories;

/// <summary>One generic read repository for every entity. Nothing it returns is tracked.</summary>
internal sealed class ReadRepository<T>(TitanFitnessDbContext context) : IReadRepository<T>
    where T : class, IEntity
{
    public IQueryable<T> GetAll() => context.Set<T>().AsNoTracking();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
