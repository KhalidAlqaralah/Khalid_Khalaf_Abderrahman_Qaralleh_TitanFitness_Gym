using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.ValueObjects;
using FluentValidation;

namespace TitanFitness.Application.Branches.CreateBranch;

public sealed record CreateBranchCommand(
    string Name,
    string? Address,
    TimeOnly Opens,
    TimeOnly Closes) : IRequest<Guid>;

public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(200);
    }
}

public sealed class CreateBranchCommandHandler(
    IBranchRepository branches,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBranchCommand, Guid>
{
    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = new Branch(request.Name, request.Address, new OperatingHours(request.Opens, request.Closes));

        branches.Add(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }
}