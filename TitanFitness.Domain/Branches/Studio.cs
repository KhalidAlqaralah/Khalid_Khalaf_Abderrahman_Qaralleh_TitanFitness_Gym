using TitanFitness.Domain.Common;

namespace TitanFitness.Domain.Branches;

public sealed class Studio
{
    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }

    private Studio() { }

    internal Studio(Guid branchId, string name, int capacity)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch is required.", nameof(branchId));

        Id = Guid.CreateVersion7();
        BranchId = branchId;
        Rename(name);
        ChangeCapacity(capacity);
    }

    internal void Rename(string name) => Name = Text.Required(name, 50, nameof(name));

    internal void ChangeCapacity(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentException("Studio capacity must be at least 1.", nameof(capacity));

        Capacity = capacity;
    }
}