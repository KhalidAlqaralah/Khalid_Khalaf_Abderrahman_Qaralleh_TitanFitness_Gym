using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.MarkAttendance;

internal sealed class MarkAttendanceCommandHandler(
    IWriteRepository<ClassSession> sessions,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<MarkAttendanceCommand, Result>
{
    public async Task<Result> Handle(MarkAttendanceCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return ClassSessionErrors.NotFound;

        var marked = session.MarkAttendance(command.BookingId, command.Attended, clock.Now);
        if (marked.IsFailure)
            return marked;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
