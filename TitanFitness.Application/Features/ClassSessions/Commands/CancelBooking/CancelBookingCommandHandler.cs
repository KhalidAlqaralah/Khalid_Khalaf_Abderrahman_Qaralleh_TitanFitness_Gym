using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.CancelBooking;

internal sealed class CancelBookingCommandHandler(
    IWriteRepository<ClassSession> sessions,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CancelBookingCommand, Result<BookingCancelledResponse>>
{
    public async Task<Result<BookingCancelledResponse>> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return ClassSessionErrors.NotFound;

        var promoted = session.CancelBooking(command.BookingId, clock.Now);
        if (promoted.IsFailure)
            return promoted.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new BookingCancelledResponse(promoted.Value?.Id, promoted.Value?.MemberId);
    }
}
