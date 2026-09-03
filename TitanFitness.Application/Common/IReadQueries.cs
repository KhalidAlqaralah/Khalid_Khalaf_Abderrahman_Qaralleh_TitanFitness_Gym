namespace TitanFitness.Application.Common;

public sealed record MemberListItem(
    Guid Id, string MembershipNumber, string FullName, string Status,
    string BranchName, DateTime? LastVisit);

public sealed record DashboardSnapshot(
    int ActiveMembers, int MembersInsideNow, int FrozenMemberships, int ExpiringInSevenDays,
    int CheckInsToday, int RefusedToday, int SessionsToday, int BookingsToday,
    decimal AverageFillRateToday);

public sealed record ScheduleItem(
    Guid Id, string ClassName, DateOnly Date, TimeOnly Start, int DurationInMinutes,
    string TrainerName, string StudioName, int CapacityLimit,
    int Confirmed, int Waitlisted, string Status);

public sealed record ActivityItem(DateTime OccurredAt, string Type, string Detail);

public sealed record MemberProfile(
    Guid Id, string MembershipNumber, string FullName, string? Email, string? Phone,
    string? Address, string? PhotoUrl, DateOnly JoinedOn, Guid HomeBranchId, string HomeBranchName,
    string Status, Guid? CurrentMembershipId, string? PlanName, decimal? AgreedPrice,
    DateOnly? StartDate, DateOnly? EndDate,
    int FreezesUsed, int MaxFreezes, int FreezeDaysUsed, int MaxFreezeDays,
    int GuestPassesUsed, int GuestPassQuota,
    IReadOnlyList<ActivityItem> RecentActivity);

public sealed record BranchListItem(
    Guid Id, string Name, string? Address, TimeOnly Opens, TimeOnly Closes, int StudioCount);

public sealed record TrainerListItem(
    Guid Id, string Name, string? Email, string? Phone, bool IsActive);

public sealed record PlanListItem(
    Guid Id, string Name, decimal Price, int DurationInMonths,
    int MaxFreezeDays, int MaxFreezes, int GuestPassQuota,
    string AccessScope, bool IsPublished);

public sealed record SessionBooking(
    Guid Id, Guid MemberId, string MemberName, string MembershipNumber,
    string Status, int Position, string? Note, DateTime BookedOn);

public sealed record SessionDetail(
    Guid Id, string ClassName, string? Description,
    Guid BranchId, string BranchName, Guid StudioId, string StudioName,
    Guid TrainerId, string TrainerName,
    DateOnly Date, TimeOnly Start, TimeOnly End, int DurationInMinutes,
    int CapacityLimit, int Confirmed, int Waitlisted, int RemainingPlaces,
    int Attended, int NoShow,
    decimal FillRate, string Status,
    IReadOnlyList<SessionBooking> Bookings);

public interface IReadQueries
{
    Task<PagedResult<MemberListItem>> SearchMembersAsync(
        string? search, Guid? branchId, int page, int pageSize, DateOnly today, CancellationToken ct = default);

    Task<DashboardSnapshot> GetDashboardAsync(Guid? branchId, DateTime now, CancellationToken ct = default);

    Task<IReadOnlyList<ScheduleItem>> GetScheduleAsync(
        Guid? branchId, DateOnly date, DateTime now, CancellationToken ct = default);

    Task<MemberProfile?> GetMemberProfileAsync(Guid memberId, DateOnly today, CancellationToken ct = default);

    Task<IReadOnlyList<BranchListItem>> ListBranchesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TrainerListItem>> ListTrainersAsync(bool activeOnly, CancellationToken ct = default);

    Task<IReadOnlyList<PlanListItem>> ListPlansAsync(bool publishedOnly, CancellationToken ct = default);

    Task<SessionDetail?> GetSessionAsync(Guid sessionId, DateTime now, CancellationToken ct = default);
}