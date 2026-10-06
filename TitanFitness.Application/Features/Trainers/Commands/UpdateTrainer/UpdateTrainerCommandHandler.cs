using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Commands.UpdateTrainer;

internal sealed class UpdateTrainerCommandHandler(
    IWriteRepository<Trainer> trainers,
    IReadRepository<Branch> branches,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateTrainerCommand, Result>
{
    public async Task<Result> Handle(UpdateTrainerCommand command, CancellationToken cancellationToken)
    {
        var trainer = await trainers.GetByIdAsync(command.TrainerId, cancellationToken);
        if (trainer is null)
            return TrainerErrors.NotFound;

        if (!await branches.GetAll().AnyAsync(b => b.Id == command.BranchId, cancellationToken))
            return Error.Validation("Branch.NotFound", "Choose a branch from the list.", "branchId");

        var updated = trainer.Update(command.Name, command.Specialty, command.BranchId, command.Email, command.Phone, command.IsActive);
        if (updated.IsFailure)
            return updated;

        var email = trainer.Email.Value;
        if (await trainers.GetAll().AnyAsync(t => t.Id != trainer.Id && t.Email.Value == email, cancellationToken))
            return TrainerErrors.DuplicateEmail(email);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
