using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Sessions.CancelSession;

public sealed record CancelSessionCommand(Guid SessionId) : IRequest<Unit>;

public sealed class CancelSessionCommandValidator : AbstractValidator<CancelSessionCommand>
{
    public CancelSessionCommandValidator() => RuleFor(x => x.SessionId).NotEmpty();
}

public sealed class CancelSessionCommandHandler(
    IClassSessionRepository sessions,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelSessionCommand, Unit>
{
    public async Task<Unit> Handle(CancelSessionCommand request, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Session", request.SessionId);

        session.Cancel(DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}