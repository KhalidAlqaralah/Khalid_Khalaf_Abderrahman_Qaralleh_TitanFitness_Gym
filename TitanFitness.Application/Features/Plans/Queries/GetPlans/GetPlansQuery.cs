using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlans;

public sealed record GetPlansQuery(
    int Page,
    int PageSize,
    string? Search,
    string? SortBy,
    SortDirection SortDirection,
    IReadOnlyList<int> Durations,
    AccessScope? Access,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? IsPublished) : IRequest<PagedResult<PlanResponse>>;
