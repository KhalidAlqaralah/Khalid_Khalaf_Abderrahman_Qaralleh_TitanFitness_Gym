using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.ClassSessions.Commands.ScheduleClass;

/// <summary>Add New Class → Schedule Class. Cross-aggregate rules first, then the session's own rules.</summary>
internal sealed class ScheduleClassCommandHandler(
    IWriteRepository<ClassSession> sessions,
    ClassSessionChecks checks,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<ScheduleClassCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ScheduleClassCommand command, CancellationToken cancellationToken)
    {
        var slot = TimeSlot.Create(command.Date, command.StartTime, command.DurationInMinutes);
        if (slot.IsFailure)
            return slot.Error;

        var session = ClassSession.Schedule(command.ClassName, command.BranchId, command.TrainerId, command.StudioId,
            slot.Value, command.CapacityLimit, command.Description, currentUser.UserName, clock.Now);
        if (session.IsFailure)
            return session.Error;

        var allowed = await checks.CheckAsync(null, command.BranchId, command.TrainerId, trainerChanged: true,
            command.StudioId, slot.Value, session.Value.CapacityLimit, cancellationToken);
        if (allowed.IsFailure)
            return allowed.Error;

        sessions.Add(session.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return session.Value.Id;
    }
}
