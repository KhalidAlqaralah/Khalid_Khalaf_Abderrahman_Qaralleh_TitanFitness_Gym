using FluentValidation;

namespace TitanFitness.Application.Memberships.PurchaseMembership;

public sealed class PurchaseMembershipCommandValidator : AbstractValidator<PurchaseMembershipCommand>
{
    public PurchaseMembershipCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
    }
}