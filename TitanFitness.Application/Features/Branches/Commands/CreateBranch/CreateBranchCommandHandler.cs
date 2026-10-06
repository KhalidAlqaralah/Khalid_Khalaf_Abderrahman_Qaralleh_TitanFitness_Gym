using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.Branches.Commands.CreateBranch;

internal sealed class CreateBranchCommandHandler(
    IWriteRepository<Branch> branches,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var hours = OperatingHours.Create(command.Opens, command.Closes);
        if (hours.IsFailure)
            return hours.Error;

        var branch = Branch.Create(command.Name, command.Address, hours.Value);
        if (branch.IsFailure)
            return branch.Error;

        if (await branches.GetAll().AnyAsync(b => b.Name == branch.Value.Name, cancellationToken))
            return Error.Conflict("Branch.Duplicate", $"A branch named '{branch.Value.Name}' already exists.", "name");

        branches.Add(branch.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return branch.Value.Id;
    }
}
