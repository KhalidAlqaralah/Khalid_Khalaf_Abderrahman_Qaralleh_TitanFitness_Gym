using MediatR;
using TitanFitness.Application.Features.Dashboard.Contracts;

namespace TitanFitness.Application.Features.Dashboard.Queries.GetActiveMembers;

public sealed record GetActiveMembersQuery(Guid? BranchId) : IRequest<ActiveMembersResponse>;
