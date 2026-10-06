using FluentValidation;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Contracts;

/// <summary>Body of POST /api/memberships: sell a plan to a member.</summary>
public sealed record PurchaseMembershipRequest(Guid? MemberId, Guid? PlanId, DateOnly? StartDate);

public sealed class PurchaseMembershipRequestValidator : AbstractValidator<PurchaseMembershipRequest>
{
    public PurchaseMembershipRequestValidator()
    {
        RuleFor(x => x.MemberId).Must(id => id is not null && id != Guid.Empty).WithMessage("Member is required.");
        RuleFor(x => x.PlanId).Must(id => id is not null && id != Guid.Empty).WithMessage("Plan is required.");
        RuleFor(x => x.StartDate).NotNull().WithMessage("Start date is required.");
    }
}

/// <summary>Body of POST /api/memberships/{id}/renewals: a new membership starting the day after this one ends.</summary>
public sealed record RenewMembershipRequest(Guid? PlanId);

public sealed class RenewMembershipRequestValidator : AbstractValidator<RenewMembershipRequest>
{
    public RenewMembershipRequestValidator() =>
        RuleFor(x => x.PlanId).Must(id => id is null || id != Guid.Empty).WithMessage("Plan is not valid.");
}

/// <summary>Body of POST /api/memberships/{id}/freezes (the Freeze Membership screen).</summary>
public sealed record FreezeMembershipRequest(DateOnly? StartDate, int? DurationInMonths, FreezeReason? Reason, string? Notes);

public sealed class FreezeMembershipRequestValidator : AbstractValidator<FreezeMembershipRequest>
{
    public const int MaxMonthsOnScreen = 3;

    public FreezeMembershipRequestValidator()
    {
        RuleFor(x => x.StartDate).NotNull().WithMessage("Start date is required.");
        RuleFor(x => x.DurationInMonths)
            .NotNull().WithMessage("Duration is required.")
            .InclusiveBetween(1, MaxMonthsOnScreen).WithMessage("Choose 1, 2 or 3 months.");
        RuleFor(x => x.Reason)
            .NotNull().WithMessage("Reason for freeze is required.")
            .IsInEnum().WithMessage("Reason for freeze is not valid.");
        RuleFor(x => x.Notes).MaximumLength(Freeze.NotesMaxLength);
    }
}

/// <summary>Body of POST /api/memberships/{id}/freezes/{freezeId}/end.</summary>
public sealed record EndFreezeRequest(DateOnly? EndedOn);

public sealed class EndFreezeRequestValidator : AbstractValidator<EndFreezeRequest>
{
    public EndFreezeRequestValidator() => RuleFor(x => x.EndedOn).NotNull().WithMessage("End date is required.");
}

/// <summary>Body of POST /api/memberships/{id}/guest-passes/{passId}/use.</summary>
public sealed record UseGuestPassRequest(string? GuestName);

public sealed class UseGuestPassRequestValidator : AbstractValidator<UseGuestPassRequest>
{
    public UseGuestPassRequestValidator() => RuleFor(x => x.GuestName).MaximumLength(GuestPass.GuestNameMaxLength);
}
