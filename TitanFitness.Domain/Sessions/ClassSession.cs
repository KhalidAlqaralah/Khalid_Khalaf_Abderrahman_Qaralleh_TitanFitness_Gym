using TitanFitness.Domain.Common;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Sessions;

public sealed class ClassSession
{
    private readonly List<Booking> _bookings = [];

    public Guid Id { get; private set; }
    public string ClassName { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Guid StudioId { get; private set; }
    public Guid TrainerId { get; private set; }
    public TimeSlot Slot { get; private set; } = null!;
    public int CapacityLimit { get; private set; }
    public SessionStatus Status { get; private set; }
    public string? Description { get; private set; }

    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private ClassSession() { }

    public static ClassSession Schedule(
        string className,
        Guid branchId,
        Guid studioId,
        Guid trainerId,
        TimeSlot slot,
        int capacityLimit,
        int studioCapacity,
        string? description,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch is required.", nameof(branchId));

        if (studioId == Guid.Empty)
            throw new ArgumentException("Studio is required.", nameof(studioId));

        if (trainerId == Guid.Empty)
            throw new ArgumentException("Trainer is required.", nameof(trainerId));

        if (capacityLimit < 1)
            throw new ArgumentException("Capacity must be at least 1.", nameof(capacityLimit));

        if (capacityLimit > studioCapacity)
            throw new InvalidOperationException(
                $"Capacity ({capacityLimit}) cannot exceed the studio's capacity ({studioCapacity}).");

        if (slot.StartsAt <= now)
            throw new InvalidOperationException("A session cannot be scheduled in the past.");

        return new ClassSession
        {
            Id = Guid.CreateVersion7(),
            ClassName = Text.Required(className, 100, nameof(className)),
            BranchId = branchId,
            StudioId = studioId,
            TrainerId = trainerId,
            Slot = slot,
            CapacityLimit = capacityLimit,
            Description = Text.Optional(description, 500, nameof(description)),
            Status = SessionStatus.Open
        };
    }

    // ---------- derived facts ----------

    public int ConfirmedCount => _bookings.Count(b => b.HoldsAPlace);
    public int WaitlistCount => _bookings.Count(b => b.Status is BookingStatus.Waitlisted);
    public int AttendedCount => _bookings.Count(b => b.Status is BookingStatus.Attended);
    public int NoShowCount => _bookings.Count(b => b.Status is BookingStatus.NoShow);
    public bool IsFull => ConfirmedCount >= CapacityLimit;
    public int RemainingPlaces => Math.Max(0, CapacityLimit - ConfirmedCount);

    public decimal FillRate =>
        CapacityLimit == 0 ? 0m : Math.Round((decimal)ConfirmedCount / CapacityLimit, 4);

    public IReadOnlyList<Booking> Waitlist =>
        _bookings.Where(b => b.Status is BookingStatus.Waitlisted)
                 .OrderBy(b => b.Position)
                 .ToList();

    public void RefreshStatus(DateTime now)
    {
        if (Status is SessionStatus.Cancelled) return;

        Status = Slot.HasFinishedBy(now) ? SessionStatus.Completed
               : Slot.HasStartedBy(now)  ? SessionStatus.InProgress
               : SessionStatus.Open;
    }

    // ---------- booking ----------

    public Booking Book(Guid memberId, string? note, DateTime now)
    {
        RefreshStatus(now);

        if (Status is not SessionStatus.Open)
            throw new InvalidOperationException($"A {Status} session accepts no further bookings.");

        if (_bookings.Any(b => b.MemberId == memberId && b.IsActive))
            throw new InvalidOperationException("This member already holds a place on this session.");

        var position = _bookings.Count == 0 ? 1 : _bookings.Max(b => b.Position) + 1;
        var booking = new Booking(Id, memberId, position, note, now);

        if (!IsFull) booking.Confirm();

        _bookings.Add(booking);
        return booking;
    }

    public Booking? CancelBooking(Guid bookingId, DateTime now)
    {
        var booking = _bookings.SingleOrDefault(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("That booking does not belong to this session.");

        var freedAPlace = booking.HoldsAPlace;
        booking.Cancel(now);

        if (!freedAPlace) return null;

        var promoted = Waitlist.FirstOrDefault();
        promoted?.Confirm();
        return promoted;
    }

    public void MarkAttendance(Guid bookingId, bool attended, DateTime now)
    {
        RefreshStatus(now);

        if (Status is SessionStatus.Cancelled)
            throw new InvalidOperationException("A cancelled session has no attendance.");

        if (Status is SessionStatus.Open)
            throw new InvalidOperationException("Attendance cannot be marked before the session starts.");

        var booking = _bookings.SingleOrDefault(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("That booking does not belong to this session.");

        if (attended) booking.MarkAttended(); else booking.MarkNoShow();
    }

    // ---------- lifecycle ----------

    public void UpdateDetails(string className, string? description)
    {
        ClassName = Text.Required(className, 100, nameof(className));
        Description = Text.Optional(description, 500, nameof(description));
    }

    public void Cancel(DateTime now)
    {
        if (Status is SessionStatus.Cancelled)
            throw new InvalidOperationException("This session is already cancelled.");

        foreach (var booking in _bookings.Where(b => b.IsActive))
            booking.Cancel(now);

        Status = SessionStatus.Cancelled;
    }
}