using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Branches.AddStudio;

public sealed record AddStudioCommand(Guid BranchId, string Name, int Capacity) : IRequest<Guid>;

public sealed class AddStudioCommandValidator : AbstractValidator<AddStudioCommand>
{
    public AddStudioCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacity).GreaterThan(0);
    }
}

public sealed class AddStudioCommandHandler(
    IBranchRepository branches,
    IUnitOfWork unitOfWork) : IRequestHandler<AddStudioCommand, Guid>
{
    public async Task<Guid> Handle(AddStudioCommand request, CancellationToken ct)
    {
        var branch = await branches.GetByIdAsync(request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var studio = branch.AddStudio(request.Name, request.Capacity);
        await unitOfWork.SaveChangesAsync(ct);

        return studio.Id;
    }
}