using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.Branches.Commands.UpdateStudio;

internal sealed class UpdateStudioCommandHandler(
    IWriteRepository<Branch> branches,
    IReadRepository<ClassSession> sessions,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateStudioCommand, Result>
{
    public async Task<Result> Handle(UpdateStudioCommand command, CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return BranchErrors.NotFound;

        // A room cannot shrink below a class already scheduled in it.
        var largestClass = await sessions.GetAll()
            .Where(s => s.StudioId == command.StudioId && s.CancelledAt == null)
            .MaxAsync(s => (int?)s.CapacityLimit, cancellationToken);

        if (largestClass is not null && command.Capacity < largestClass)
            return Error.Conflict("Studio.CapacityInUse",
                $"A class in this room is set to {largestClass} spots, so its capacity cannot go below that.", "capacity");

        var updated = branch.UpdateStudio(command.StudioId, command.Name, command.Capacity);
        if (updated.IsFailure)
            return updated;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
