using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.CancelClass;

internal sealed class CancelClassCommandHandler(
    IWriteRepository<ClassSession> sessions,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CancelClassCommand, Result>
{
    public async Task<Result> Handle(CancelClassCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return ClassSessionErrors.NotFound;

        var cancelled = session.Cancel(clock.Now);
        if (cancelled.IsFailure)
            return cancelled;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
