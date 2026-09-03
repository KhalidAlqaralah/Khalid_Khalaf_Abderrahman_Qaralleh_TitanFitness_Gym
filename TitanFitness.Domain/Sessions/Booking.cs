using TitanFitness.Domain.Common;

namespace TitanFitness.Domain.Sessions;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateTime BookedOn { get; private set; }
    public int Position { get; private set; }
    public BookingStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTime? CancelledOn { get; private set; }

    private Booking() { }

    internal Booking(Guid sessionId, Guid memberId, int position, string? note, DateTime now)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session is required.", nameof(sessionId));

        if (memberId == Guid.Empty)
            throw new ArgumentException("Member is required.", nameof(memberId));

        Id = Guid.CreateVersion7();
        SessionId = sessionId;
        MemberId = memberId;
        Position = position;
        BookedOn = now;
        Status = BookingStatus.Waitlisted;
        Note = Text.Optional(note, 500, nameof(note));
    }

    public bool IsActive => Status is not BookingStatus.Cancelled;

    public bool HoldsAPlace =>
        Status is BookingStatus.Confirmed or BookingStatus.Attended or BookingStatus.NoShow;

    internal void Confirm()
    {
        if (Status is not BookingStatus.Waitlisted)
            throw new InvalidOperationException("Only a waitlisted booking can be confirmed.");

        Status = BookingStatus.Confirmed;
    }

    internal void Cancel(DateTime now)
    {
        if (Status is BookingStatus.Cancelled)
            throw new InvalidOperationException("This booking is already cancelled.");

        Status = BookingStatus.Cancelled;
        CancelledOn = now;
    }

    internal void MarkAttended()
    {
        if (!HoldsAPlace)
            throw new InvalidOperationException("Only a confirmed booking can be marked as attended.");

        Status = BookingStatus.Attended;
    }

    internal void MarkNoShow()
    {
        if (!HoldsAPlace)
            throw new InvalidOperationException("Only a confirmed booking can be marked as a no show.");

        Status = BookingStatus.NoShow;
    }
}