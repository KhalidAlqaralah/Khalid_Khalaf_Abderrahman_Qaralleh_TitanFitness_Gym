using FluentValidation;

namespace TitanFitness.Application.Members.CreateMember;

public sealed class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.MembershipNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.HomeBranchId).NotEmpty();
    }
}