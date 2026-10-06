using FluentValidation;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Plans.Contracts;

/// <summary>
/// Body of POST /api/plans and PUT /api/plans/{id}. Empty optional numbers are saved as 0 and
/// an empty access scope as Home branch only, as the Plan Details screen describes.
/// </summary>
public sealed record PlanRequest(
    string Name,
    decimal? Price,
    int? DurationInMonths,
    bool IsPublished,
    int? MaxFreezeDays,
    int? MaxFreezes,
    int? GuestPassQuota,
    AccessScope? AccessScope);

public sealed class PlanRequestValidator : AbstractValidator<PlanRequest>
{
    public PlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Plan name is required.")
            .Must(n => n is null || n.Trim().Length is >= Plan.NameMinLength and <= Plan.NameMaxLength)
            .WithMessage($"Plan name must be {Plan.NameMinLength}–{Plan.NameMaxLength} characters.");

        RuleFor(x => x.Price)
            .NotNull().WithMessage("Price is required.")
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.")
            .Must(p => p is null || decimal.Round(p.Value, 2) == p.Value).WithMessage("Price can have at most 2 decimal places.");

        RuleFor(x => x.DurationInMonths)
            .NotNull().WithMessage("Duration in months is required.")
            .InclusiveBetween(MembershipTerms.MinDuration, MembershipTerms.MaxDuration)
            .WithMessage($"Duration must be a whole number between {MembershipTerms.MinDuration} and {MembershipTerms.MaxDuration}.");

        RuleFor(x => x.MaxFreezeDays).GreaterThanOrEqualTo(0).WithMessage("Maximum freeze days cannot be negative.");
        RuleFor(x => x.MaxFreezes).GreaterThanOrEqualTo(0).WithMessage("Maximum number of freezes cannot be negative.");
        RuleFor(x => x.GuestPassQuota).GreaterThanOrEqualTo(0).WithMessage("Guest pass quota cannot be negative.");

        RuleFor(x => x.MaxFreezes)
            .Must((request, freezes) => (request.MaxFreezeDays ?? 0) > 0 || (freezes ?? 0) == 0)
            .WithMessage("Maximum number of freezes must be 0 when maximum freeze days is 0.");

        RuleFor(x => x.AccessScope).IsInEnum().When(x => x.AccessScope is not null);
    }
}

/// <summary>[FromQuery] object for GET /api/plans.</summary>
public sealed class GetPlansRequest : Common.PagedRequest
{
    public List<int>? Durations { get; init; }

    public AccessScope? Access { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    /// <summary>"Published", "Retired" or both. Empty means all.</summary>
    public List<string>? Statuses { get; init; }
}

public sealed class GetPlansRequestValidator : AbstractValidator<GetPlansRequest>
{
    private static readonly string[] SortColumns = ["name", "price", "duration", "guestPasses", "access", "status"];

    public GetPlansRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Common.PagedRequest.MaxPageSize);
        RuleFor(x => x.SortBy)
            .Must(s => s is null || SortColumns.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortColumns)}.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice).When(x => x.MinPrice is not null && x.MaxPrice is not null)
            .WithMessage("Max price cannot be below min price.");
        RuleForEach(x => x.Statuses)
            .Must(s => s is "Published" or "Retired").WithMessage("Status must be Published or Retired.");
    }
}
