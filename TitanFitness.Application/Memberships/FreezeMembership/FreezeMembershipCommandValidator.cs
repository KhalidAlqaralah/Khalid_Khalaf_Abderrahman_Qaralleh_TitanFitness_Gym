using FluentValidation;

namespace TitanFitness.Application.Memberships.FreezeMembership;

public sealed class FreezeMembershipCommandValidator : AbstractValidator<FreezeMembershipCommand>
{
    public FreezeMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.DurationInMonths).InclusiveBetween(1, 12);
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(200);
    }
}