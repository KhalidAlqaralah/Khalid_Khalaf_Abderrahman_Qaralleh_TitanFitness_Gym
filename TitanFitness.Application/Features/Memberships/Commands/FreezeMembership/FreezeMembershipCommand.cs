using MediatR;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Commands.FreezeMembership;

public sealed record FreezeMembershipCommand(Guid MembershipId, DateOnly StartDate, int DurationInMonths, FreezeReason Reason, string? Notes)
    : IRequest<Result<FreezeAppliedResponse>>;
