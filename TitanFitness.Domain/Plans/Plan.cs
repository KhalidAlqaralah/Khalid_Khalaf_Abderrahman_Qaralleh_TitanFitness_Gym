using TitanFitness.Domain.Memberships;

namespace TitanFitness.Domain.Plans;

public sealed class Plan
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public MembershipTerms Terms { get; private set; } = null!;
    public bool IsPublished { get; private set; }

    private Plan() { }

    public Plan(string name, MembershipTerms terms)
    {
        Id = Guid.CreateVersion7();
        Rename(name);
        ChangeTerms(terms);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan name is required.", nameof(name));

        var trimmed = name.Trim();

        if (trimmed.Length > 50)
            throw new ArgumentException("Plan name cannot exceed 50 characters.", nameof(name));

        Name = trimmed;
    }

    public void ChangeTerms(MembershipTerms terms)
    {
        ArgumentNullException.ThrowIfNull(terms);
        Terms = terms;
    }

    public void Publish() => IsPublished = true;

    public void Unpublish() => IsPublished = false;
}