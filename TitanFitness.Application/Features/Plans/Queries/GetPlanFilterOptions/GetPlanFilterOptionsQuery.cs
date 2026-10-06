using MediatR;
using TitanFitness.Application.Features.Plans.Contracts;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlanFilterOptions;

public sealed record GetPlanFilterOptionsQuery : IRequest<PlanFilterOptionsResponse>;
