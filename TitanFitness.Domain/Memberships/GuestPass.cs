namespace TitanFitness.Domain.Memberships;

public sealed class GuestPass
{
    public Guid Id { get; private set; }
    public Guid MembershipId { get; private set; }
    public DateOnly IssuedOn { get; private set; }
    public DateOnly? UsedOn { get; private set; }
    public string? GuestName { get; private set; }

    private GuestPass() { }

    internal GuestPass(Guid membershipId, DateOnly issuedOn)
    {
        if (membershipId == Guid.Empty)
            throw new ArgumentException("Membership is required.", nameof(membershipId));

        Id = Guid.CreateVersion7();
        MembershipId = membershipId;
        IssuedOn = issuedOn;
    }

    public bool IsUsed => UsedOn is not null;

    internal void Use(DateOnly on, string? guestName)
    {
        if (UsedOn is not null)
            throw new InvalidOperationException("This guest pass has already been used.");

        if (on < IssuedOn)
            throw new ArgumentException("A guest pass cannot be used before it was issued.", nameof(on));

        if (guestName is not null && guestName.Trim().Length > 100)
            throw new ArgumentException("Guest name cannot exceed 100 characters.", nameof(guestName));

        UsedOn = on;
        GuestName = string.IsNullOrWhiteSpace(guestName) ? null : guestName.Trim();
    }
}