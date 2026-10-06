using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.ClassSessions.Commands.UpdateClass;

internal sealed class UpdateClassCommandHandler(
    IWriteRepository<ClassSession> sessions,
    ClassSessionChecks checks,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateClassCommand, Result>
{
    public async Task<Result> Handle(UpdateClassCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return ClassSessionErrors.NotFound;

        var slot = TimeSlot.Create(command.Date, command.StartTime, command.DurationInMinutes);
        if (slot.IsFailure)
            return slot.Error;

        var trainerChanged = command.TrainerId != session.TrainerId || command.BranchId != session.BranchId;

        var updated = session.Update(command.ClassName, command.BranchId, command.TrainerId, command.StudioId,
            slot.Value, command.CapacityLimit, command.Description, clock.Now);
        if (updated.IsFailure)
            return updated;

        var allowed = await checks.CheckAsync(session.Id, command.BranchId, command.TrainerId, trainerChanged,
            command.StudioId, slot.Value, session.CapacityLimit, cancellationToken);
        if (allowed.IsFailure)
            return allowed;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
