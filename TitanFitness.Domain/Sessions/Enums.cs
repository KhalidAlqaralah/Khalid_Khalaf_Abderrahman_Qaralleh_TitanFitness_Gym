namespace TitanFitness.Domain.Sessions;

public enum BookingStatus
{
    Waitlisted = 1,
    Confirmed = 2,
    Cancelled = 3,
    Attended = 4,
    NoShow = 5
}

/// <summary>
/// What the schedule shows for a class at a given moment. Never stored: it is worked out from the
/// time slot, the enrolment and whether staff cancelled the class.
/// </summary>
public enum ClassState
{
    Upcoming = 1,
    InProgress = 2,
    Full = 3,
    Completed = 4,
    Cancelled = 5
}
