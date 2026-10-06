namespace TitanFitness.Application.Features.Trainers.Contracts;

public sealed record TrainerListItemResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Specialty { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = null!;
    public bool IsActive { get; init; }
}

public sealed record TrainerResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Specialty { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public string CreatedBy { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}

public sealed record TrainerLookupResponse(Guid Id, string Name, Guid BranchId, bool IsActive);

public sealed record TrainerCreatedResponse(Guid Id, string Code);
