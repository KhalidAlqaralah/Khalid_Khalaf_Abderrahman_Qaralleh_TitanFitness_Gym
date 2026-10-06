using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.Memberships;

/// <summary>A one-visit pass for a guest, counted against the membership's guest pass quota.</summary>
public sealed class GuestPass : Entity
{
    public const int GuestNameMaxLength = 100;

    public Guid MembershipId { get; private set; }
    public DateOnly IssuedOn { get; private set; }
    public DateOnly? UsedOn { get; private set; }
    public string? GuestName { get; private set; }

    private GuestPass()
    {
    }

    internal static GuestPass Create(Guid membershipId, DateOnly issuedOn) => new()
    {
        Id = Guid.CreateVersion7(),
        MembershipId = membershipId,
        IssuedOn = issuedOn
    };

    public bool IsUsed => UsedOn is not null;

    internal Result Use(DateOnly on, string? guestName)
    {
        if (UsedOn is not null)
            return Error.Conflict("GuestPass.AlreadyUsed", "This guest pass has already been used.");

        if (on < IssuedOn)
            return Error.Validation("GuestPass.BeforeIssue", "A guest pass cannot be used before it was issued.");

        var name = Guard.Optional(guestName, GuestNameMaxLength, "guestName", "Guest name");
        if (name.IsFailure)
            return name.Error;

        UsedOn = on;
        GuestName = name.Value;
        return Result.Success();
    }
}
