using MediatR;
using TitanFitness.Application.Features.Dashboard.Contracts;

namespace TitanFitness.Application.Features.Dashboard.Queries.GetCheckInsToday;

public sealed record GetCheckInsTodayQuery(Guid? BranchId) : IRequest<CheckInsTodayResponse>;
