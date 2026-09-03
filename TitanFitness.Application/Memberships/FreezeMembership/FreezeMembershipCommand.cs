using MediatR;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Memberships.FreezeMembership;

public sealed record FreezeMembershipCommand(
    Guid MembershipId,
    DateOnly StartDate,
    int DurationInMonths,
    FreezeReason Reason,
    string? Notes) : IRequest<Guid>;