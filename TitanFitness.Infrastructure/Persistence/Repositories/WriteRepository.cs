using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Infrastructure.Persistence.Repositories;

/// <summary>
/// One generic write repository for every aggregate root. Loaded aggregates are tracked
/// (children come along through AutoInclude) and saved by the unit of work.
/// </summary>
internal sealed class WriteRepository<T>(TitanFitnessDbContext context) : IWriteRepository<T>
    where T : class, IAggregateRoot
{
    public IQueryable<T> GetAll() => context.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(T entity) => context.Set<T>().Add(entity);

    public void AddRange(IEnumerable<T> entities) => context.Set<T>().AddRange(entities);

    public void Remove(T entity) => context.Set<T>().Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => context.Set<T>().RemoveRange(entities);
}
