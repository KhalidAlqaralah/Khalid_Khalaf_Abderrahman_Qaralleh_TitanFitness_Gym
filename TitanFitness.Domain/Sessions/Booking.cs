using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.Sessions;

/// <summary>
/// A member's place on a class. Owned by <see cref="ClassSession"/>, which decides whether a new
/// booking is confirmed or goes on the waitlist.
/// </summary>
public sealed class Booking : Entity
{
    public const int NoteMaxLength = 500;

    public Guid SessionId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateTime BookedOn { get; private set; }
    public int Position { get; private set; }
    public BookingStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTime? CancelledOn { get; private set; }

    private Booking()
    {
    }

    internal static Result<Booking> Create(Guid sessionId, Guid memberId, int position, string? note, DateTime now)
    {
        var member = Guard.RequiredId(memberId, "memberId", "Member");
        if (member.IsFailure)
            return member.Error;

        var cleanNote = Guard.Optional(note, NoteMaxLength, "note", "Note");
        if (cleanNote.IsFailure)
            return cleanNote.Error;

        return new Booking
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            MemberId = memberId,
            Position = position,
            BookedOn = now,
            Status = BookingStatus.Waitlisted,
            Note = cleanNote.Value
        };
    }

    public bool IsActive => Status is not BookingStatus.Cancelled;

    public bool HoldsAPlace => Status is BookingStatus.Confirmed or BookingStatus.Attended or BookingStatus.NoShow;

    internal void Confirm() => Status = BookingStatus.Confirmed;

    internal Result Cancel(DateTime now)
    {
        if (Status is BookingStatus.Cancelled)
            return Error.Conflict("Booking.AlreadyCancelled", "This booking is already cancelled.");

        Status = BookingStatus.Cancelled;
        CancelledOn = now;
        return Result.Success();
    }

    internal Result MarkAttendance(bool attended)
    {
        if (!HoldsAPlace)
            return Error.Conflict("Booking.NotConfirmed", "Only a confirmed booking can have attendance marked.");

        Status = attended ? BookingStatus.Attended : BookingStatus.NoShow;
        return Result.Success();
    }
}
