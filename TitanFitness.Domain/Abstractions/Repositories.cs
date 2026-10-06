namespace TitanFitness.Domain.Abstractions;

/// <summary>
/// Generic read side. Any entity, root or child, can be read. Queries are never tracked.
/// </summary>
public interface IReadRepository<T> where T : class, IEntity
{
    /// <summary>Composable, untracked query. Nothing runs until the caller materialises it.</summary>
    IQueryable<T> GetAll();

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic write side. Only aggregate roots can be added or removed, and their
/// children always come along with them. Queries are tracked; there is no Update:
/// change a tracked aggregate through its methods and the unit of work saves the difference.
/// </summary>
public interface IWriteRepository<T> where T : class, IAggregateRoot
{
    /// <summary>Composable, tracked query.</summary>
    IQueryable<T> GetAll();

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(T entity);

    void AddRange(IEnumerable<T> entities);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Commits every change made during one use case in a single transaction.
/// Repositories are injected separately; this only commits.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
