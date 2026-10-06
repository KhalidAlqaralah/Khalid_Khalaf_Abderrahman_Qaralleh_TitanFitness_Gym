using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetCapacityOverview;

public sealed record GetCapacityOverviewQuery(Guid? BranchId, DateOnly? Date, int Days) : IRequest<CapacityOverviewResponse>;
