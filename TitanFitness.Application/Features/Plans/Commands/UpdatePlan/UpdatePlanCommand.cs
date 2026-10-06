using MediatR;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Plans.Commands.UpdatePlan;

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    decimal Price,
    int DurationInMonths,
    bool IsPublished,
    int MaxFreezeDays,
    int MaxFreezes,
    int GuestPassQuota,
    AccessScope AccessScope) : IRequest<Result>;
