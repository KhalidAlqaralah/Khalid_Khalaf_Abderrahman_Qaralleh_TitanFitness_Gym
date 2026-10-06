using MediatR;
using TitanFitness.Application.Features.Plans.Contracts;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlanLookup;

/// <summary>Published plans only: what front desk can sell from the member profile.</summary>
public sealed record GetPlanLookupQuery : IRequest<IReadOnlyList<PlanResponse>>;
