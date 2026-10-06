using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Memberships;

/// <summary>
/// What a plan sells, and what a membership was sold. One immutable value object used in two places:
/// <see cref="Plans.Plan"/> holds the terms it sells today, <see cref="Membership"/> holds a copy of the
/// terms taken at purchase. Editing a plan replaces its object and never reaches a sold membership.
/// </summary>
public sealed record MembershipTerms
{
    public const int MinDuration = 1;
    public const int MaxDuration = 36;

    public Money Price { get; private set; } = null!;
    public int DurationInMonths { get; private set; }
    public int MaxFreezeDays { get; private set; }
    public int MaxFreezes { get; private set; }
    public int GuestPassQuota { get; private set; }
    public AccessScope AccessScope { get; private set; }

    private MembershipTerms()
    {
    }

    private MembershipTerms(Money price, int duration, int freezeDays, int freezes, int guestPasses, AccessScope scope)
    {
        Price = price;
        DurationInMonths = duration;
        MaxFreezeDays = freezeDays;
        MaxFreezes = freezes;
        GuestPassQuota = guestPasses;
        AccessScope = scope;
    }

    public static Result<MembershipTerms> Create(
        Money price,
        int durationInMonths,
        int maxFreezeDays,
        int maxFreezes,
        int guestPassQuota,
        AccessScope accessScope)
    {
        if (durationInMonths is < MinDuration or > MaxDuration)
            return Error.Validation("Terms.Duration",
                $"Duration must be a whole number of months between {MinDuration} and {MaxDuration}.", "durationInMonths");

        if (maxFreezeDays < 0)
            return Error.Validation("Terms.FreezeDays", "Maximum freeze days cannot be negative.", "maxFreezeDays");

        if (maxFreezes < 0)
            return Error.Validation("Terms.Freezes", "Maximum number of freezes cannot be negative.", "maxFreezes");

        if (maxFreezeDays == 0 && maxFreezes > 0)
            return Error.Validation("Terms.FreezesWithoutDays",
                "Maximum number of freezes must be 0 when maximum freeze days is 0.", "maxFreezes");

        if (maxFreezeDays > 0 && maxFreezes == 0)
            return Error.Validation("Terms.DaysWithoutFreezes",
                "Freeze days are offered, so at least one freeze must be allowed.", "maxFreezes");

        if (guestPassQuota < 0)
            return Error.Validation("Terms.GuestPasses", "Guest pass quota cannot be negative.", "guestPassQuota");

        if (!Enum.IsDefined(accessScope))
            return Error.Validation("Terms.AccessScope", "Access scope is not valid.", "accessScope");

        return new MembershipTerms(price, durationInMonths, maxFreezeDays, maxFreezes, guestPassQuota, accessScope);
    }

    /// <summary>A separate copy with the same values (no shared instances): the snapshot a membership keeps.</summary>
    internal MembershipTerms Snapshot() =>
        new(Money.Create(Price.Amount).Value, DurationInMonths, MaxFreezeDays, MaxFreezes, GuestPassQuota, AccessScope);
}
