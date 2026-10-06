using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.Branches.Commands.UpdateBranch;

internal sealed class UpdateBranchCommandHandler(
    IWriteRepository<Branch> branches,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBranchCommand, Result>
{
    public async Task<Result> Handle(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return BranchErrors.NotFound;

        var hours = OperatingHours.Create(command.Opens, command.Closes);
        if (hours.IsFailure)
            return hours.Error;

        var name = command.Name.Trim();
        if (await branches.GetAll().AnyAsync(b => b.Id != branch.Id && b.Name == name, cancellationToken))
            return Error.Conflict("Branch.Duplicate", $"A branch named '{name}' already exists.", "name");

        var updated = branch.Update(command.Name, command.Address, hours.Value);
        if (updated.IsFailure)
            return updated;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
