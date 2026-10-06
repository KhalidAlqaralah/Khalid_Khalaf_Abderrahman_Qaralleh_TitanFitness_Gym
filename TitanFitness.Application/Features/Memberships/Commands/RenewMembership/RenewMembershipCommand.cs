using MediatR;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Commands.RenewMembership;

/// <summary>Sells the next membership, starting the day after this one ends (or today if it already ended).</summary>
public sealed record RenewMembershipCommand(Guid MembershipId, Guid? PlanId) : IRequest<Result<MembershipCreatedResponse>>;
