using MediatR;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Queries.GetMembershipById;

public sealed record GetMembershipByIdQuery(Guid MembershipId) : IRequest<Result<MembershipResponse>>;
