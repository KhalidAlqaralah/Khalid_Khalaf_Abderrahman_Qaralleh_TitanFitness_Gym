using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Branches.UpdateStudio;

public sealed record UpdateStudioCommand(
    Guid BranchId, Guid StudioId, string Name, int Capacity) : IRequest<Unit>;

public sealed class UpdateStudioCommandValidator : AbstractValidator<UpdateStudioCommand>
{
    public UpdateStudioCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacity).GreaterThan(0);
    }
}

public sealed class UpdateStudioCommandHandler(
    IBranchRepository branches,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateStudioCommand, Unit>
{
    public async Task<Unit> Handle(UpdateStudioCommand request, CancellationToken ct)
    {
        var branch = await branches.GetByIdAsync(request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        branch.RenameStudio(request.StudioId, request.Name);
        branch.ChangeStudioCapacity(request.StudioId, request.Capacity);

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}