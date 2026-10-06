using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Commands.CreateTrainer;

/// <summary>Adds a trainer. The system gives it the next TR-NNNN ID and records who created it and when.</summary>
internal sealed class CreateTrainerCommandHandler(
    IWriteRepository<Trainer> trainers,
    IReadRepository<Branch> branches,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<CreateTrainerCommand, Result<TrainerCreatedResponse>>
{
    public async Task<Result<TrainerCreatedResponse>> Handle(CreateTrainerCommand command, CancellationToken cancellationToken)
    {
        if (!await branches.GetAll().AnyAsync(b => b.Id == command.BranchId, cancellationToken))
            return Error.Validation("Branch.NotFound", "Choose a branch from the list.", "branchId");

        var lastCode = await trainers.GetAll()
            .Where(t => t.Code.StartsWith(Trainer.CodePrefix))
            .OrderByDescending(t => t.Code.Length)
            .ThenByDescending(t => t.Code)
            .Select(t => t.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var code = Trainer.CodeFor(Math.Max(Trainer.SequenceOf(lastCode), 1000) + 1);

        var trainer = Trainer.Create(code, command.Name, command.Specialty, command.BranchId, command.Email,
            command.Phone, command.IsActive, currentUser.UserName, clock.Now);
        if (trainer.IsFailure)
            return trainer.Error;

        var email = trainer.Value.Email.Value;
        if (await trainers.GetAll().AnyAsync(t => t.Email.Value == email, cancellationToken))
            return TrainerErrors.DuplicateEmail(email);

        trainers.Add(trainer.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TrainerCreatedResponse(trainer.Value.Id, trainer.Value.Code);
    }
}
