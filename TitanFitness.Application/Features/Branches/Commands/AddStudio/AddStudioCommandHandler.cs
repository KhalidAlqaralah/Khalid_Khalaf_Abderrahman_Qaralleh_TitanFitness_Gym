using MediatR;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;

namespace TitanFitness.Application.Features.Branches.Commands.AddStudio;

internal sealed class AddStudioCommandHandler(
    IWriteRepository<Branch> branches,
    IUnitOfWork unitOfWork) : IRequestHandler<AddStudioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddStudioCommand command, CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return BranchErrors.NotFound;

        var studio = branch.AddStudio(command.Name, command.Capacity);
        if (studio.IsFailure)
            return studio.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return studio.Value.Id;
    }
}
