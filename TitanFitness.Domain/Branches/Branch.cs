using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Branches;

/// <summary>
/// A gym location. Owns its studios: a studio can only be added or changed through its branch,
/// which is what keeps studio names unique inside one branch.
/// </summary>
public sealed class Branch : AggregateRoot
{
    public const int NameMaxLength = 50;
    public const int AddressMaxLength = 200;

    private readonly List<Studio> _studios = [];

    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public OperatingHours Hours { get; private set; } = null!;

    public IReadOnlyCollection<Studio> Studios => _studios.AsReadOnly();

    private Branch()
    {
    }

    public static Result<Branch> Create(string name, string? address, OperatingHours hours)
    {
        var branch = new Branch { Id = Guid.CreateVersion7() };

        var applied = branch.Apply(name, address, hours);
        if (applied.IsFailure)
            return applied.Error;

        return branch;
    }

    public Result Update(string name, string? address, OperatingHours hours) => Apply(name, address, hours);

    public Result<Studio> AddStudio(string name, int capacity)
    {
        var studio = Studio.Create(Id, name, capacity);
        if (studio.IsFailure)
            return studio.Error;

        if (HasStudioNamed(studio.Value.Name, exceptStudioId: null))
            return BranchErrors.DuplicateStudio(studio.Value.Name);

        _studios.Add(studio.Value);
        return studio.Value;
    }

    public Result UpdateStudio(Guid studioId, string name, int capacity)
    {
        var studio = FindStudio(studioId);
        if (studio is null)
            return BranchErrors.StudioNotFound;

        var trimmed = name?.Trim() ?? string.Empty;
        if (HasStudioNamed(trimmed, exceptStudioId: studioId))
            return BranchErrors.DuplicateStudio(trimmed);

        return studio.Update(name ?? string.Empty, capacity);
    }

    public Studio? FindStudio(Guid studioId) => _studios.SingleOrDefault(s => s.Id == studioId);

    private bool HasStudioNamed(string name, Guid? exceptStudioId) =>
        _studios.Any(s => s.Id != exceptStudioId && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private Result Apply(string name, string? address, OperatingHours hours)
    {
        var cleanName = Guard.Required(name, NameMaxLength, "name", "Branch name");
        if (cleanName.IsFailure)
            return cleanName.Error;

        var cleanAddress = Guard.Optional(address, AddressMaxLength, "address", "Address");
        if (cleanAddress.IsFailure)
            return cleanAddress.Error;

        Name = cleanName.Value;
        Address = cleanAddress.Value;
        Hours = hours;
        return Result.Success();
    }
}

public static class BranchErrors
{
    public static readonly Error NotFound = Error.NotFound("Branch.NotFound", "The branch was not found.");

    public static readonly Error StudioNotFound = Error.NotFound("Studio.NotFound", "The studio was not found in this branch.");

    public static Error DuplicateStudio(string name) =>
        Error.Conflict("Studio.Duplicate", $"This branch already has a studio named '{name}'.", "name");
}
