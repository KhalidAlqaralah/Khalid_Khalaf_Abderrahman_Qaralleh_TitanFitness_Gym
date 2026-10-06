using MediatR;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Commands.PurchaseMembership;

public sealed record PurchaseMembershipCommand(Guid MemberId, Guid PlanId, DateOnly StartDate) : IRequest<Result<MembershipCreatedResponse>>;
