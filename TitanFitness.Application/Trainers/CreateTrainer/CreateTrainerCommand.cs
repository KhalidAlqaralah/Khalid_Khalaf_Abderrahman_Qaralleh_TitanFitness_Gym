using FluentValidation;
using MediatR;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Trainers.CreateTrainer;

public sealed record CreateTrainerCommand(string Name, string? Email, string? Phone) : IRequest<Guid>;

public sealed class CreateTrainerCommandValidator : AbstractValidator<CreateTrainerCommand>
{
    public CreateTrainerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public sealed class CreateTrainerCommandHandler(
    ITrainerRepository trainers,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTrainerCommand, Guid>
{
    public async Task<Guid> Handle(CreateTrainerCommand request, CancellationToken ct)
    {
        var trainer = new Trainer(request.Name);
        trainer.UpdateContactDetails(request.Email, request.Phone);

        trainers.Add(trainer);
        await unitOfWork.SaveChangesAsync(ct);

        return trainer.Id;
    }
}