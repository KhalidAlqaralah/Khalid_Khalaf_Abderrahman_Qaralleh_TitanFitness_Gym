using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Domain.Plans;

/// <summary>
/// A membership offering in the catalogue. Only a published plan can be sold;
/// a retired (unpublished) plan stays in the catalogue for history.
/// </summary>
public sealed class Plan : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 60;

    public string Name { get; private set; } = null!;
    public MembershipTerms Terms { get; private set; } = null!;
    public bool IsPublished { get; private set; }

    private Plan()
    {
    }

    public static Result<Plan> Create(string name, MembershipTerms terms, bool isPublished)
    {
        var plan = new Plan { Id = Guid.CreateVersion7() };

        var applied = plan.Update(name, terms, isPublished);
        if (applied.IsFailure)
            return applied.Error;

        return plan;
    }

    /// <summary>
    /// Replaces the terms this plan sells. Memberships already sold hold their own copy and do not change.
    /// </summary>
    public Result Update(string name, MembershipTerms terms, bool isPublished)
    {
        var cleanName = Guard.Required(name, NameMaxLength, "name", "Plan name");
        if (cleanName.IsFailure)
            return cleanName.Error;

        if (cleanName.Value.Length < NameMinLength)
            return Error.Validation("Plan.NameTooShort", $"Plan name must be at least {NameMinLength} characters.", "name");

        Name = cleanName.Value;
        Terms = terms;
        IsPublished = isPublished;
        return Result.Success();
    }
}

public static class PlanErrors
{
    public static readonly Error NotFound = Error.NotFound("Plan.NotFound", "The plan was not found.");

    public static Error DuplicateName(string name) =>
        Error.Conflict("Plan.DuplicateName", $"A plan named '{name}' already exists.", "name");
}
