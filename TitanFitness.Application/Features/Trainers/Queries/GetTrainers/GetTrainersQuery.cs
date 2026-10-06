using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Trainers.Contracts;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainers;

public sealed record GetTrainersQuery(
    int Page,
    int PageSize,
    string? Search,
    string? SortBy,
    SortDirection SortDirection,
    IReadOnlyList<Guid> BranchIds,
    IReadOnlyList<string> Specialties,
    bool? IsActive) : IRequest<PagedResult<TrainerListItemResponse>>;
