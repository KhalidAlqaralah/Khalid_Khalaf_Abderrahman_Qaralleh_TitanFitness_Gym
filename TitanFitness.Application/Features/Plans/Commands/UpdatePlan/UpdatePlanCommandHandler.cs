using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.Plans.Commands.UpdatePlan;

/// <summary>Changes what a plan sells from now on. Memberships already sold keep their own copy of the terms.</summary>
internal sealed class UpdatePlanCommandHandler(
    IWriteRepository<Plan> plans,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePlanCommand, Result>
{
    public async Task<Result> Handle(UpdatePlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await plans.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return PlanErrors.NotFound;

        var price = Money.Create(command.Price);
        if (price.IsFailure)
            return price.Error;

        var terms = MembershipTerms.Create(price.Value, command.DurationInMonths, command.MaxFreezeDays,
            command.MaxFreezes, command.GuestPassQuota, command.AccessScope);
        if (terms.IsFailure)
            return terms.Error;

        var name = command.Name.Trim();
        if (await plans.GetAll().AnyAsync(p => p.Id != plan.Id && p.Name == name, cancellationToken))
            return PlanErrors.DuplicateName(name);

        var updated = plan.Update(command.Name, terms.Value, command.IsPublished);
        if (updated.IsFailure)
            return updated;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
