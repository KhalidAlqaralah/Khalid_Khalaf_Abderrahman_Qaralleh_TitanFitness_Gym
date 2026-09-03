using TitanFitness.Domain.Common;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Branches;

public sealed class Branch
{
    private readonly List<Studio> _studios = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public OperatingHours Hours { get; private set; } = null!;

    public IReadOnlyCollection<Studio> Studios => _studios.AsReadOnly();

    private Branch() { }

    public Branch(string name, string? address, OperatingHours hours)
    {
        Id = Guid.CreateVersion7();
        Rename(name);
        UpdateAddress(address);
        ChangeHours(hours);
    }

    public void Rename(string name) => Name = Text.Required(name, 50, nameof(name));

    public void UpdateAddress(string? address) => Address = Text.Optional(address, 200, nameof(address));

    public void ChangeHours(OperatingHours hours)
    {
        ArgumentNullException.ThrowIfNull(hours);
        Hours = hours;
    }

    public Studio AddStudio(string name, int capacity)
    {
        var studio = new Studio(Id, name, capacity);

        if (_studios.Any(s => s.Name.Equals(studio.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"This branch already has a studio named '{studio.Name}'.");

        _studios.Add(studio);
        return studio;
    }

    public void RenameStudio(Guid studioId, string name)
    {
        var studio = GetStudio(studioId);

        if (_studios.Any(s => s.Id != studioId && s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"This branch already has a studio named '{name.Trim()}'.");

        studio.Rename(name);
    }

    public void ChangeStudioCapacity(Guid studioId, int capacity) => GetStudio(studioId).ChangeCapacity(capacity);

    public Studio GetStudio(Guid studioId) =>
        _studios.SingleOrDefault(s => s.Id == studioId)
        ?? throw new InvalidOperationException("That studio does not belong to this branch.");
}