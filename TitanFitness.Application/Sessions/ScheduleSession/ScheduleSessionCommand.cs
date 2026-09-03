using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Sessions.ScheduleSession;

public sealed record ScheduleSessionCommand(
    string ClassName,
    Guid BranchId,
    Guid StudioId,
    Guid TrainerId,
    DateOnly SessionDate,
    TimeOnly StartTime,
    int DurationInMinutes,
    int CapacityLimit,
    string? Description) : IRequest<Guid>;

public sealed class ScheduleSessionCommandValidator : AbstractValidator<ScheduleSessionCommand>
{
    public ScheduleSessionCommandValidator()
    {
        RuleFor(x => x.ClassName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.DurationInMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.CapacityLimit).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class ScheduleSessionCommandHandler(
    IBranchRepository branches,
    ITrainerRepository trainers,
    IClassSessionRepository sessions,
    IUnitOfWork unitOfWork) : IRequestHandler<ScheduleSessionCommand, Guid>
{
    public async Task<Guid> Handle(ScheduleSessionCommand request, CancellationToken ct)
    {
        var branch = await branches.GetByIdAsync(request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var studio = branch.Studios.SingleOrDefault(s => s.Id == request.StudioId)
            ?? throw new NotFoundException("Studio", request.StudioId);

        var trainer = await trainers.GetByIdAsync(request.TrainerId, ct)
            ?? throw new NotFoundException("Trainer", request.TrainerId);

        if (!trainer.IsActive)
            throw new InvalidOperationException("That trainer is not active.");

        var slot = new TimeSlot(request.SessionDate, request.StartTime, request.DurationInMinutes);

        if (!branch.Hours.Covers(slot.Start, slot.End))
            throw new InvalidOperationException(
                $"The session falls outside branch hours ({branch.Hours}).");

        if (await sessions.TrainerIsBusyAsync(trainer.Id, slot, null, ct))
            throw new InvalidOperationException("That trainer already runs an overlapping session.");

        if (await sessions.StudioIsBusyAsync(studio.Id, slot, null, ct))
            throw new InvalidOperationException("That studio is already booked at that time.");

        var session = ClassSession.Schedule(
            request.ClassName, branch.Id, studio.Id, trainer.Id,
            slot, request.CapacityLimit, studio.Capacity,
            request.Description, DateTime.Now);

        sessions.Add(session);
        await unitOfWork.SaveChangesAsync(ct);

        return session.Id;
    }
}