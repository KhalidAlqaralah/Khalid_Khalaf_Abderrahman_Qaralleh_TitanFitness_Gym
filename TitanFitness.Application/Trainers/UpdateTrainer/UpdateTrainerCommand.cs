using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Trainers.UpdateTrainer;

public sealed record UpdateTrainerCommand(
    Guid TrainerId, string Name, string? Email, string? Phone, bool IsActive) : IRequest<Unit>;

public sealed class UpdateTrainerCommandValidator : AbstractValidator<UpdateTrainerCommand>
{
    public UpdateTrainerCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public sealed class UpdateTrainerCommandHandler(
    ITrainerRepository trainers,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateTrainerCommand, Unit>
{
    public async Task<Unit> Handle(UpdateTrainerCommand request, CancellationToken ct)
    {
        var trainer = await trainers.GetByIdAsync(request.TrainerId, ct)
            ?? throw new NotFoundException("Trainer", request.TrainerId);

        trainer.Rename(request.Name);
        trainer.UpdateContactDetails(request.Email, request.Phone);

        if (request.IsActive) trainer.Activate(); else trainer.Deactivate();

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}