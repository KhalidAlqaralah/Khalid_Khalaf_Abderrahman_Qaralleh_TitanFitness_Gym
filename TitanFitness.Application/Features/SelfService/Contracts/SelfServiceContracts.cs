using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.SelfService.Contracts;

/// <summary>The Eligibility Status box on Book Session — Member View.</summary>
public sealed record EligibilityResponse(
    Guid MemberId,
    string MemberName,
    string MembershipNumber,
    MemberStatus Status,
    bool Eligible,
    string Title,
    string Detail,
    BookingStatus? ExistingBooking);
