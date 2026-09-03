using MediatR;

namespace TitanFitness.Application.Memberships.PurchaseMembership;

public sealed record PurchaseMembershipCommand(
    Guid MemberId,
    Guid PlanId,
    DateOnly StartDate) : IRequest<Guid>;