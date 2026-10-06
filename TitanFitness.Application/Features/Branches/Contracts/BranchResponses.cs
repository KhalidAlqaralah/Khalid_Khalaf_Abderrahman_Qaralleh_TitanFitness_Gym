namespace TitanFitness.Application.Features.Branches.Contracts;

public sealed record StudioResponse(Guid Id, string Name, int Capacity);

public sealed record BranchResponse(Guid Id, string Name, string? Address, TimeOnly Opens, TimeOnly Closes, IReadOnlyList<StudioResponse> Studios);
