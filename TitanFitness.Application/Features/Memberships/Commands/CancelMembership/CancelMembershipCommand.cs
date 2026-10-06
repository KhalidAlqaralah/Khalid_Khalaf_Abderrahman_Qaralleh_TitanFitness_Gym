using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Commands.CancelMembership;

public sealed record CancelMembershipCommand(Guid MembershipId) : IRequest<Result>;
