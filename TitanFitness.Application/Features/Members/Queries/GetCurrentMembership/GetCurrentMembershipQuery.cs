using MediatR;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Members.Queries.GetCurrentMembership;

public sealed record GetCurrentMembershipQuery(Guid MemberId) : IRequest<Result<MembershipResponse>>;
