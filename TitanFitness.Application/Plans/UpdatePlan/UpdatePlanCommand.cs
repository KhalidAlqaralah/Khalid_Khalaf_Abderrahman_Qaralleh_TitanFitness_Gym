using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Plans.UpdatePlan;

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    decimal Price,
    int DurationInMonths,
    int MaxFreezeDays,
    int MaxFreezes,
    int GuestPassQuota,
    AccessScope AccessScope,
    bool IsPublished) : IRequest<Unit>;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationInMonths).InclusiveBetween(1, 36);
        RuleFor(x => x.MaxFreezeDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxFreezes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GuestPassQuota).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccessScope).IsInEnum();
    }
}

public sealed class UpdatePlanCommandHandler(
    IPlanRepository plans,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePlanCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePlanCommand request, CancellationToken ct)
    {
        var plan = await plans.GetByIdAsync(request.PlanId, ct)
            ?? throw new NotFoundException("Plan", request.PlanId);

        var terms = new MembershipTerms(
            new Money(request.Price),
            request.DurationInMonths,
            request.MaxFreezeDays,
            request.MaxFreezes,
            request.GuestPassQuota,
            request.AccessScope);

        plan.Rename(request.Name);
        plan.ChangeTerms(terms);

        if (request.IsPublished) plan.Publish(); else plan.Unpublish();

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}