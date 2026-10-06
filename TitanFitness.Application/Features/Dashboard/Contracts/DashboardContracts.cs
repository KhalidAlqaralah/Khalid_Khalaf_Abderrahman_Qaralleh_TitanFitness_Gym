using FluentValidation;

namespace TitanFitness.Application.Features.Dashboard.Contracts;

/// <summary>[FromQuery] object for the dashboard KPI endpoints. Empty branch means the whole chain.</summary>
public sealed class DashboardRequest
{
    public Guid? BranchId { get; init; }
}

/// <summary>[FromQuery] object for GET /api/dashboard/upcoming-classes.</summary>
public sealed class UpcomingClassesRequest
{
    public Guid? BranchId { get; init; }

    public int Take { get; init; } = 5;
}

public sealed class UpcomingClassesRequestValidator : AbstractValidator<UpcomingClassesRequest>
{
    public UpcomingClassesRequestValidator() => RuleFor(x => x.Take).InclusiveBetween(1, 20);
}

/// <summary>Check-ins Today card: today's count against the same day last week.</summary>
public sealed record CheckInsTodayResponse(int Today, int SameDayLastWeek, decimal? ChangePercent);

/// <summary>Active Members card: members holding an active (not frozen) membership, and how many are in the building.</summary>
public sealed record ActiveMembersResponse(int ActiveMembers, int OnFloor, int OnFloorWindowMinutes);
