using TitanFitness.Domain.Common;

namespace TitanFitness.Domain.Trainers;

public sealed class Trainer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }

    private Trainer() { }

    public Trainer(string name)
    {
        Id = Guid.CreateVersion7();
        Rename(name);
        IsActive = true;
    }

    public void Rename(string name) => Name = Text.Required(name, 100, nameof(name));

    public void UpdateContactDetails(string? email, string? phone)
    {
        Email = Text.Optional(email, 100, nameof(email));
        Phone = Text.Optional(phone, 20, nameof(phone));
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}