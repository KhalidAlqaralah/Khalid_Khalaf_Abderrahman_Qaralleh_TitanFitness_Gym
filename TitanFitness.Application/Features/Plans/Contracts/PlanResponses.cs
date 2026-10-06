using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Plans.Contracts;

/// <summary>One plan, for the catalogue rows and the Plan Details screen.</summary>
public sealed record PlanResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public decimal Price { get; init; }
    public int DurationInMonths { get; init; }
    public int MaxFreezeDays { get; init; }
    public int MaxFreezes { get; init; }
    public int GuestPassQuota { get; init; }
    public AccessScope AccessScope { get; init; }
    public bool IsPublished { get; init; }
}

/// <summary>What the Filter Plans dialog offers: the distinct durations and the price bounds.</summary>
public sealed record PlanFilterOptionsResponse(IReadOnlyList<int> Durations, decimal MinPrice, decimal MaxPrice);
