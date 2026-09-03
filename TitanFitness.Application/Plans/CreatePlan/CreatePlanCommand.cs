using FluentValidation;
using MediatR;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Plans.CreatePlan;

public sealed record CreatePlanCommand(
    string Name,
    decimal Price,
    int DurationInMonths,
    int MaxFreezeDays,
    int MaxFreezes,
    int GuestPassQuota,
    AccessScope AccessScope,
    bool Publish) : IRequest<Guid>;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationInMonths).InclusiveBetween(1, 36);
        RuleFor(x => x.MaxFreezeDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxFreezes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GuestPassQuota).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccessScope).IsInEnum();
    }
}

public sealed class CreatePlanCommandHandler(
    IPlanRepository plans,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePlanCommand, Guid>
{
    public async Task<Guid> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var terms = new MembershipTerms(
            new Money(request.Price),
            request.DurationInMonths,
            request.MaxFreezeDays,
            request.MaxFreezes,
            request.GuestPassQuota,
            request.AccessScope);

        var plan = new Plan(request.Name, terms);

        if (request.Publish) plan.Publish();

        plans.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}