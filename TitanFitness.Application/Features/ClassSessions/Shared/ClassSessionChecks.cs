using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.ClassSessions.Shared;

/// <summary>
/// The scheduling rules that need other aggregates, so they cannot live inside <see cref="ClassSession"/>:
/// the room belongs to the branch and is big enough, the trainer is active at that branch, and neither the
/// trainer nor the room is already used for an overlapping slot.
/// </summary>
internal sealed class ClassSessionChecks(
    IReadRepository<Branch> branches,
    IReadRepository<Trainer> trainers,
    IReadRepository<ClassSession> sessions)
{
    public async Task<Result> CheckAsync(
        Guid? sessionId,
        Guid branchId,
        Guid? trainerId,
        bool trainerChanged,
        Guid? studioId,
        TimeSlot slot,
        int capacityLimit,
        CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken);
        if (branch is null)
            return Error.Validation("Branch.NotFound", "Choose a branch from the list.", "branchId");

        if (studioId is not null)
        {
            var studio = branch.FindStudio(studioId.Value);
            if (studio is null)
                return Error.Validation("Studio.WrongBranch", "Choose a room at the selected branch.", "studioId");

            if (capacityLimit > studio.Capacity)
                return ClassSessionErrors.CapacityAboveStudio(studio.Capacity);
        }

        if (trainerId is not null)
        {
            var trainer = await trainers.GetByIdAsync(trainerId.Value, cancellationToken);
            if (trainer is null)
                return Error.Validation("Trainer.NotFound", "Choose an instructor from the list.", "trainerId");

            if (trainerChanged && !trainer.IsActive)
                return TrainerErrors.NotActive;

            if (trainerChanged && trainer.BranchId != branchId)
                return TrainerErrors.WrongBranch;
        }

        if (trainerId is null && studioId is null)
            return Result.Success();

        var sameDay = await sessions.GetAll()
            .Where(s => s.Slot.Date == slot.Date
                     && s.CancelledAt == null
                     && (sessionId == null || s.Id != sessionId)
                     && ((trainerId != null && s.TrainerId == trainerId) || (studioId != null && s.StudioId == studioId)))
            .Select(s => new { s.TrainerId, s.StudioId, s.Slot })
            .ToListAsync(cancellationToken);

        var clashing = sameDay.Where(s => s.Slot.Overlaps(slot)).ToList();

        if (trainerId is not null && clashing.Any(s => s.TrainerId == trainerId))
            return TrainerErrors.Busy;

        if (studioId is not null && clashing.Any(s => s.StudioId == studioId))
            return ClassSessionErrors.StudioBusy;

        return Result.Success();
    }
}
