using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainers;

/// <summary>Trainer Directory: filter (Where) → join + project (Select) → sort (OrderBy) → page (Skip/Take) → ToListAsync.</summary>
internal sealed class GetTrainersQueryHandler(
    IReadRepository<Trainer> trainers,
    IReadRepository<Branch> branches) : IRequestHandler<GetTrainersQuery, PagedResult<TrainerListItemResponse>>
{
    public Task<PagedResult<TrainerListItemResponse>> Handle(GetTrainersQuery query, CancellationToken cancellationToken)
    {
        var source = trainers.GetAll();

        if (query.BranchIds.Count > 0)
            source = source.Where(t => query.BranchIds.Contains(t.BranchId));

        if (query.Specialties.Count > 0)
            source = source.Where(t => t.Specialty != null && query.Specialties.Contains(t.Specialty));

        if (query.IsActive is not null)
            source = source.Where(t => t.IsActive == query.IsActive);

        var rows =
            from t in source
            join b in branches.GetAll() on t.BranchId equals b.Id
            select new TrainerListItemResponse
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Specialty = t.Specialty,
                BranchId = t.BranchId,
                BranchName = b.Name,
                IsActive = t.IsActive
            };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().TrimStart('#');
            rows = rows.Where(r => r.Name.Contains(term) || r.Code.Contains(term)
                                || (r.Specialty != null && r.Specialty.Contains(term)) || r.BranchName.Contains(term));
        }

        var ordered = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => rows.OrderByDirection(r => r.Code, query.SortDirection),
            "specialty" => rows.OrderByDirection(r => r.Specialty, query.SortDirection),
            "branch" => rows.OrderByDirection(r => r.BranchName, query.SortDirection),
            "status" => rows.OrderByDirection(r => r.IsActive, query.SortDirection),
            _ => rows.OrderByDirection(r => r.Name, query.SortDirection)
        };

        return ordered.ThenBy(r => r.Code).ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
