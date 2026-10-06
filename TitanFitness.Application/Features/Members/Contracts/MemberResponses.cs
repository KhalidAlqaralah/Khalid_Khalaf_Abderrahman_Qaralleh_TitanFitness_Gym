using TitanFitness.Application.Features.Memberships.Shared;

namespace TitanFitness.Application.Features.Members.Contracts;

public sealed record MemberListItemResponse
{
    public Guid Id { get; init; }
    public string MembershipNumber { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string? PhotoUrl { get; init; }
    public MemberStatus Status { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = null!;
    public DateTime? LastVisit { get; init; }

    /// <summary>Freezes left on the current membership; the row's Freeze action needs at least one.</summary>
    public int RemainingFreezes { get; init; }
}

public sealed record MemberResponse
{
    public Guid Id { get; init; }
    public string MembershipNumber { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public DateOnly JoinedOn { get; init; }
    public string? PhotoUrl { get; init; }
    public Guid HomeBranchId { get; init; }
    public string HomeBranchName { get; init; } = null!;
    public MemberStatus Status { get; init; }
    public DateTime? LastVisit { get; init; }
    public string CreatedBy { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}

public sealed record MemberCreatedResponse(Guid Id, string MembershipNumber);

public enum ActivityKind
{
    CheckIn = 1,
    ClassAttendance = 2
}

public sealed record MemberActivityResponse(ActivityKind Kind, string Title, string Detail, DateTime OccurredAt);
