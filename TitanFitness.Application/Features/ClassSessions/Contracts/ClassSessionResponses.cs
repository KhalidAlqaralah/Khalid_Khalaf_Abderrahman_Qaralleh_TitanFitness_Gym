using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Contracts;

/// <summary>One class row: Class Schedule, Dashboard Upcoming Classes, and the class dialogs.</summary>
public sealed record ClassSessionResponse
{
    public Guid Id { get; init; }
    public string ClassName { get; init; } = null!;
    public string? Description { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = null!;
    public Guid? TrainerId { get; init; }
    public string? TrainerName { get; init; }
    public Guid? StudioId { get; init; }
    public string? StudioName { get; init; }
    public int? StudioCapacity { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public int DurationInMinutes { get; init; }
    public TimeOnly EndTime => StartTime.AddMinutes(DurationInMinutes);
    public int CapacityLimit { get; init; }
    public int Enrolled { get; init; }
    public int Waitlist { get; init; }
    public int ActiveBookings { get; init; }
    public bool IsCancelled { get; init; }
    public ClassState State { get; init; }
    public int RemainingPlaces => Math.Max(0, CapacityLimit - Enrolled);
}

public sealed record CapacityOverviewResponse(int SessionCount, int TotalBookings, decimal AverageFillRate);

public sealed record SessionBookingResponse
{
    public Guid BookingId { get; init; }
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = null!;
    public string MembershipNumber { get; init; } = null!;
    public BookingStatus Status { get; init; }
    public int Position { get; init; }
    public string? Note { get; init; }
    public DateTime BookedOn { get; init; }
}

public sealed record BookingResultResponse(Guid BookingId, BookingStatus Status, int? WaitlistPosition);

public sealed record BookingCancelledResponse(Guid? PromotedBookingId, Guid? PromotedMemberId);
