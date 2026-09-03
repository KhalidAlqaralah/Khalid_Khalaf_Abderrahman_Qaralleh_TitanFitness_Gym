using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Memberships;

public sealed record MembershipTerms
{
    public Money Price { get; private set; } = null!;
    public int DurationInMonths { get; private set; }
    public int MaxFreezeDays { get; private set; }
    public int MaxFreezes { get; private set; }
    public int GuestPassQuota { get; private set; }
    public AccessScope AccessScope { get; private set; }

    private MembershipTerms() { }

    public MembershipTerms(
        Money price,
        int durationInMonths,
        int maxFreezeDays,
        int maxFreezes,
        int guestPassQuota,
        AccessScope accessScope)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (durationInMonths < 1)
            throw new ArgumentException("Duration must be at least 1 month.", nameof(durationInMonths));

        if (maxFreezeDays < 0)
            throw new ArgumentException("Max freeze days cannot be negative.", nameof(maxFreezeDays));

        if (maxFreezes < 0)
            throw new ArgumentException("Max freezes cannot be negative.", nameof(maxFreezes));

        if (guestPassQuota < 0)
            throw new ArgumentException("Guest pass quota cannot be negative.", nameof(guestPassQuota));

        if (maxFreezeDays > 0 && maxFreezes == 0)
            throw new ArgumentException("Freeze days offered but no freezes allowed.", nameof(maxFreezes));

        Price = price;
        DurationInMonths = durationInMonths;
        MaxFreezeDays = maxFreezeDays;
        MaxFreezes = maxFreezes;
        GuestPassQuota = guestPassQuota;
        AccessScope = accessScope;
    }
}