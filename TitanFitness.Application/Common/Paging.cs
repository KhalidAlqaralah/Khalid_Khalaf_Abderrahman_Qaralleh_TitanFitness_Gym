namespace TitanFitness.Application.Common;

public enum SortDirection
{
    Asc = 1,
    Desc = 2
}

/// <summary>Base for every list request bound with [FromQuery]: page, size, search and sort in one object.</summary>
public abstract class PagedRequest
{
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class QueryableExtensions
{
    /// <summary>
    /// The paging step at the end of every list query: count the filtered rows, then Skip/Take the requested page.
    /// The query passed in is already filtered, projected and ordered.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> orderedQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, PagedRequest.MaxPageSize);

        var total = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(orderedQuery, cancellationToken);

        var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            orderedQuery.Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);

        return new PagedResult<T>(items, page, pageSize, total);
    }

    /// <summary>OrderBy or OrderByDescending depending on the requested direction.</summary>
    public static IOrderedQueryable<T> OrderByDirection<T, TKey>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TKey>> key,
        SortDirection direction) =>
        direction == SortDirection.Desc ? query.OrderByDescending(key) : query.OrderBy(key);
}
