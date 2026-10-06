using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlans;

/// <summary>Plan Catalogue: filter (Where) → project (Select) → sort (OrderBy) → page (Skip/Take) → ToListAsync.</summary>
internal sealed class GetPlansQueryHandler(IReadRepository<Plan> plans)
    : IRequestHandler<GetPlansQuery, PagedResult<PlanResponse>>
{
    public Task<PagedResult<PlanResponse>> Handle(GetPlansQuery query, CancellationToken cancellationToken)
    {
        var source = plans.GetAll();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(p => p.Name.Contains(term));
        }

        if (query.Durations.Count > 0)
            source = source.Where(p => query.Durations.Contains(p.Terms.DurationInMonths));

        if (query.Access is not null)
            source = source.Where(p => p.Terms.AccessScope == query.Access);

        if (query.IsPublished is not null)
            source = source.Where(p => p.IsPublished == query.IsPublished);

        if (query.MinPrice is not null)
            source = source.Where(p => p.Terms.Price.Amount >= query.MinPrice);

        if (query.MaxPrice is not null)
            source = source.Where(p => p.Terms.Price.Amount <= query.MaxPrice);

        IQueryable<PlanResponse> rows = source.Select(p => new PlanResponse
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Terms.Price.Amount,
            DurationInMonths = p.Terms.DurationInMonths,
            MaxFreezeDays = p.Terms.MaxFreezeDays,
            MaxFreezes = p.Terms.MaxFreezes,
            GuestPassQuota = p.Terms.GuestPassQuota,
            AccessScope = p.Terms.AccessScope,
            IsPublished = p.IsPublished
        });

        var ordered = (query.SortBy?.ToLowerInvariant()) switch
        {
            "price" => rows.OrderByDirection(p => p.Price, query.SortDirection),
            "duration" => rows.OrderByDirection(p => p.DurationInMonths, query.SortDirection),
            "guestpasses" => rows.OrderByDirection(p => p.GuestPassQuota, query.SortDirection),
            "access" => rows.OrderByDirection(p => p.AccessScope, query.SortDirection),
            "status" => rows.OrderByDirection(p => p.IsPublished, query.SortDirection),
            _ => rows.OrderByDirection(p => p.Name, query.SortDirection)
        };

        return ordered.ThenBy(p => p.Id).ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
