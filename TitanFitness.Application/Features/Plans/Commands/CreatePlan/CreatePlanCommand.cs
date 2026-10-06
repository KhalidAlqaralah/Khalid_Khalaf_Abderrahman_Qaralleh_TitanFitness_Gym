using MediatR;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Plans.Commands.CreatePlan;

public sealed record CreatePlanCommand(
    string Name,
    decimal Price,
    int DurationInMonths,
    bool IsPublished,
    int MaxFreezeDays,
    int MaxFreezes,
    int GuestPassQuota,
    AccessScope AccessScope) : IRequest<Result<Guid>>;
