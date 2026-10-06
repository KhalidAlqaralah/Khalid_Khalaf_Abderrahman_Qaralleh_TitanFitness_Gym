using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.Branches;

/// <summary>
/// A room inside a branch. Child entity of <see cref="Branch"/>: created and changed only by the branch.
/// </summary>
public sealed class Studio : Entity
{
    public const int NameMaxLength = 50;
    public const int MaxCapacity = 500;

    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }

    private Studio()
    {
    }

    internal static Result<Studio> Create(Guid branchId, string name, int capacity)
    {
        var studio = new Studio { Id = Guid.CreateVersion7(), BranchId = branchId };

        var applied = studio.Update(name, capacity);
        if (applied.IsFailure)
            return applied.Error;

        return studio;
    }

    internal Result Update(string name, int capacity)
    {
        var cleanName = Guard.Required(name, NameMaxLength, "name", "Studio name");
        if (cleanName.IsFailure)
            return cleanName.Error;

        if (capacity is < 1 or > MaxCapacity)
            return Error.Validation("Studio.Capacity", $"Studio capacity must be between 1 and {MaxCapacity}.", "capacity");

        Name = cleanName.Value;
        Capacity = capacity;
        return Result.Success();
    }
}
