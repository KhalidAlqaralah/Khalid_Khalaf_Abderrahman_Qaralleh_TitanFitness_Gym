using TitanFitness.Domain.Memberships;

namespace TitanFitness.Api.Controllers;

public sealed record FreezeMembershipRequest(
    DateOnly StartDate,
    int DurationInMonths,
    FreezeReason Reason,
    string? Notes);