using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Sessions;

/// <summary>
/// One scheduled class. Owns its bookings so it can enforce capacity and run the waitlist.
/// Rules that need other aggregates (studio capacity, trainer or studio double booking,
/// member eligibility) are checked by the application layer before calling in here.
/// </summary>
public sealed class ClassSession : AggregateRoot
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 500;
    public const int MinCapacity = 1;
    public const int MaxCapacity = 100;
    public const int DefaultCapacity = 20;
    public static readonly int[] AllowedDurations = [30, 45, 60];

    private readonly List<Booking> _bookings = [];

    public string ClassName { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Guid? StudioId { get; private set; }
    public Guid? TrainerId { get; private set; }
    public TimeSlot Slot { get; private set; } = null!;
    public int CapacityLimit { get; private set; }
    public string? Description { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private ClassSession()
    {
    }

    public static Result<ClassSession> Schedule(
        string className,
        Guid branchId,
        Guid? trainerId,
        Guid? studioId,
        TimeSlot slot,
        int? capacityLimit,
        string? description,
        string createdBy,
        DateTime now)
    {
        var session = new ClassSession
        {
            Id = Guid.CreateVersion7(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
            CreatedAt = now
        };

        var applied = session.Apply(className, branchId, trainerId, studioId, slot, capacityLimit, description, now);
        if (applied.IsFailure)
            return applied.Error;

        return session;
    }

    // ---------- derived facts ----------

    public bool IsCancelled => CancelledAt is not null;
    public int ConfirmedCount => _bookings.Count(b => b.HoldsAPlace);
    public int WaitlistCount => _bookings.Count(b => b.Status is BookingStatus.Waitlisted);
    public int ActiveBookingCount => _bookings.Count(b => b.IsActive);
    public bool IsFull => ConfirmedCount >= CapacityLimit;
    public int RemainingPlaces => Math.Max(0, CapacityLimit - ConfirmedCount);

    public IReadOnlyList<Booking> Waitlist =>
        _bookings.Where(b => b.Status is BookingStatus.Waitlisted).OrderBy(b => b.Position).ToList();

    public ClassState StateAt(DateTime now) => StateOf(IsCancelled, Slot.StartsAt, Slot.EndsAt, ConfirmedCount, CapacityLimit, now);

    /// <summary>The state rule from the requirements, usable on raw values read by a query.</summary>
    public static ClassState StateOf(bool cancelled, DateTime startsAt, DateTime endsAt, int enrolled, int capacity, DateTime now) =>
        cancelled ? ClassState.Cancelled
        : now >= endsAt ? ClassState.Completed
        : now >= startsAt ? ClassState.InProgress
        : enrolled >= capacity ? ClassState.Full
        : ClassState.Upcoming;

    // ---------- changes ----------

    /// <summary>
    /// Edit Class. The branch can only change while nobody is booked, and capacity can never drop
    /// below the places already taken.
    /// </summary>
    public Result Update(
        string className,
        Guid branchId,
        Guid? trainerId,
        Guid? studioId,
        TimeSlot slot,
        int? capacityLimit,
        string? description,
        DateTime now)
    {
        if (IsCancelled)
            return ClassSessionErrors.Cancelled;

        if (StateAt(now) is ClassState.Completed)
            return ClassSessionErrors.Completed;

        if (branchId != BranchId && ActiveBookingCount > 0)
            return ClassSessionErrors.BranchLocked;

        var capacity = capacityLimit ?? DefaultCapacity;
        if (capacity < ConfirmedCount)
            return ClassSessionErrors.CapacityBelowEnrolment(ConfirmedCount);

        var slotChanged = slot != Slot;
        var applied = Apply(className, branchId, trainerId, studioId, slot, capacity, description, slotChanged ? now : null);
        if (applied.IsFailure)
            return applied;

        PromoteFromWaitlist();
        return Result.Success();
    }

    public Result<Booking> Book(Guid memberId, string? note, DateTime now)
    {
        var state = StateAt(now);
        if (state is ClassState.Cancelled or ClassState.Completed or ClassState.InProgress)
            return ClassSessionErrors.NotBookable(state);

        if (_bookings.Any(b => b.MemberId == memberId && b.IsActive))
            return ClassSessionErrors.AlreadyBooked;

        var position = _bookings.Count == 0 ? 1 : _bookings.Max(b => b.Position) + 1;
        var booking = Booking.Create(Id, memberId, position, note, now);
        if (booking.IsFailure)
            return booking.Error;

        if (!IsFull)
            booking.Value.Confirm();

        _bookings.Add(booking.Value);
        return booking.Value;
    }

    /// <summary>Cancels a booking. When it held a place, the first person on the waitlist gets it.</summary>
    public Result<Booking?> CancelBooking(Guid bookingId, DateTime now)
    {
        var booking = _bookings.SingleOrDefault(b => b.Id == bookingId);
        if (booking is null)
            return ClassSessionErrors.BookingNotFound;

        var freedAPlace = booking.HoldsAPlace;
        var cancelled = booking.Cancel(now);
        if (cancelled.IsFailure)
            return cancelled.Error;

        if (!freedAPlace)
            return Result.Success<Booking?>(null);

        var promoted = Waitlist.FirstOrDefault();
        promoted?.Confirm();
        return Result.Success(promoted);
    }

    public Result MarkAttendance(Guid bookingId, bool attended, DateTime now)
    {
        var state = StateAt(now);
        if (state is ClassState.Cancelled)
            return ClassSessionErrors.Cancelled;

        if (state is ClassState.Upcoming or ClassState.Full)
            return ClassSessionErrors.NotStarted;

        var booking = _bookings.SingleOrDefault(b => b.Id == bookingId);
        if (booking is null)
            return ClassSessionErrors.BookingNotFound;

        return booking.MarkAttendance(attended);
    }

    public Result Cancel(DateTime now)
    {
        if (IsCancelled)
            return ClassSessionErrors.Cancelled;

        if (StateAt(now) is ClassState.Completed)
            return ClassSessionErrors.Completed;

        foreach (var booking in _bookings.Where(b => b.IsActive))
            booking.Cancel(now);

        CancelledAt = now;
        return Result.Success();
    }

    private void PromoteFromWaitlist()
    {
        foreach (var waiting in Waitlist)
        {
            if (IsFull)
                break;

            waiting.Confirm();
        }
    }

    /// <summary>Shared by Schedule and Update. <paramref name="now"/> is null when the slot did not change.</summary>
    private Result Apply(
        string className,
        Guid branchId,
        Guid? trainerId,
        Guid? studioId,
        TimeSlot slot,
        int? capacityLimit,
        string? description,
        DateTime? now)
    {
        var name = Guard.Required(className, NameMaxLength, "className", "Class name");
        if (name.IsFailure)
            return name.Error;

        if (name.Value.Length < NameMinLength)
            return Error.Validation("Class.NameTooShort", $"Class name must be at least {NameMinLength} characters.", "className");

        var branch = Guard.RequiredId(branchId, "branchId", "Branch");
        if (branch.IsFailure)
            return branch.Error;

        if (!AllowedDurations.Contains(slot.DurationInMinutes))
            return Error.Validation("Class.Duration", "Duration must be 30, 45 or 60 minutes.", "durationInMinutes");

        var capacity = capacityLimit ?? DefaultCapacity;
        if (capacity is < MinCapacity or > MaxCapacity)
            return Error.Validation("Class.Capacity", $"Capacity must be a whole number between {MinCapacity} and {MaxCapacity}.", "capacityLimit");

        if (now is not null && slot.StartsAt <= now.Value)
            return slot.Date < DateOnly.FromDateTime(now.Value)
                ? Error.Validation("Class.DateInPast", "The date must be today or later.", "date")
                : Error.Validation("Class.TimeInPast", "The start time must be later than now.", "startTime");

        var cleanDescription = Guard.Optional(description, DescriptionMaxLength, "description", "Description");
        if (cleanDescription.IsFailure)
            return cleanDescription.Error;

        ClassName = name.Value;
        BranchId = branch.Value;
        TrainerId = trainerId == Guid.Empty ? null : trainerId;
        StudioId = studioId == Guid.Empty ? null : studioId;
        Slot = slot;
        CapacityLimit = capacity;
        Description = cleanDescription.Value;
        return Result.Success();
    }
}

public static class ClassSessionErrors
{
    public static readonly Error NotFound = Error.NotFound("Class.NotFound", "Class not found.");

    public static readonly Error Cancelled = Error.Conflict("Class.Cancelled", "This class has been cancelled.");

    public static readonly Error Completed = Error.Conflict("Class.Completed", "This class has already finished.");

    public static readonly Error NotStarted =
        Error.Conflict("Class.NotStarted", "Attendance cannot be marked before the class starts.");

    public static readonly Error BranchLocked =
        Error.Conflict("Class.BranchLocked", "The branch cannot change once the class has bookings.", "branchId");

    public static readonly Error AlreadyBooked =
        Error.Conflict("Booking.Duplicate", "This member already holds a place on this class.", "memberId");

    public static readonly Error BookingNotFound =
        Error.NotFound("Booking.NotFound", "That booking does not belong to this class.");

    public static readonly Error StudioBusy =
        Error.Conflict("Studio.Busy", "This room is already booked for an overlapping slot.", "studioId");

    public static Error NotBookable(ClassState state) =>
        Error.Conflict("Class.NotBookable", $"A class that is {Describe(state)} accepts no further bookings.");

    public static Error CapacityBelowEnrolment(int enrolled) =>
        Error.Validation("Class.CapacityBelowEnrolment", $"Capacity cannot be below the current enrolment ({enrolled}).", "capacityLimit");

    public static Error CapacityAboveStudio(int studioCapacity) =>
        Error.Validation("Class.CapacityAboveStudio", $"Capacity cannot exceed the room's capacity ({studioCapacity}).", "capacityLimit");

    private static string Describe(ClassState state) => state switch
    {
        ClassState.InProgress => "in progress",
        _ => state.ToString().ToLowerInvariant()
    };
}
