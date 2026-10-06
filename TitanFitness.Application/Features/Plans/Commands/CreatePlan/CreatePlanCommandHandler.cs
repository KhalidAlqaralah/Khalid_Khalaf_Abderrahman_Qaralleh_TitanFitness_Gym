using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.Plans.Commands.CreatePlan;

internal sealed class CreatePlanCommandHandler(
    IWriteRepository<Plan> plans,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePlanCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        var price = Money.Create(command.Price);
        if (price.IsFailure)
            return price.Error;

        var terms = MembershipTerms.Create(price.Value, command.DurationInMonths, command.MaxFreezeDays,
            command.MaxFreezes, command.GuestPassQuota, command.AccessScope);
        if (terms.IsFailure)
            return terms.Error;

        var plan = Plan.Create(command.Name, terms.Value, command.IsPublished);
        if (plan.IsFailure)
            return plan.Error;

        var name = plan.Value.Name;
        if (await plans.GetAll().AnyAsync(p => p.Name == name, cancellationToken))
            return PlanErrors.DuplicateName(name);

        plans.Add(plan.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return plan.Value.Id;
    }
}
