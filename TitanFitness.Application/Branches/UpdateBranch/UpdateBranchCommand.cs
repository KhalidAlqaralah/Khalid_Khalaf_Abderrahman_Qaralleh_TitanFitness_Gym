using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Branches.UpdateBranch;

public sealed record UpdateBranchCommand(
    Guid BranchId, string Name, string? Address, TimeOnly Opens, TimeOnly Closes) : IRequest<Unit>;

public sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(200);
    }
}

public sealed class UpdateBranchCommandHandler(
    IBranchRepository branches,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBranchCommand, Unit>
{
    public async Task<Unit> Handle(UpdateBranchCommand request, CancellationToken ct)
    {
        var branch = await branches.GetByIdAsync(request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        branch.Rename(request.Name);
        branch.UpdateAddress(request.Address);
        branch.ChangeHours(new OperatingHours(request.Opens, request.Closes));

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}