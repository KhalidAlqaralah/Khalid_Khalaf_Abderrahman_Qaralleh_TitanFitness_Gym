using MediatR;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlanById;

public sealed record GetPlanByIdQuery(Guid PlanId) : IRequest<Result<PlanResponse>>;
