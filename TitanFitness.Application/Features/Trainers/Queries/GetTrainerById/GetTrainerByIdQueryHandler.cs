using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainerById;

internal sealed class GetTrainerByIdQueryHandler(
    IReadRepository<Trainer> trainers,
    IReadRepository<Branch> branches) : IRequestHandler<GetTrainerByIdQuery, Result<TrainerResponse>>
{
    public async Task<Result<TrainerResponse>> Handle(GetTrainerByIdQuery query, CancellationToken cancellationToken)
    {
        var trainer = await (
            from t in trainers.GetAll()
            join b in branches.GetAll() on t.BranchId equals b.Id
            where t.Id == query.TrainerId
            select new TrainerResponse
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Specialty = t.Specialty,
                BranchId = t.BranchId,
                BranchName = b.Name,
                Email = t.Email.Value,
                Phone = t.Phone,
                IsActive = t.IsActive,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt
            }).FirstOrDefaultAsync(cancellationToken);

        return trainer is null ? TrainerErrors.NotFound : trainer;
    }
}
