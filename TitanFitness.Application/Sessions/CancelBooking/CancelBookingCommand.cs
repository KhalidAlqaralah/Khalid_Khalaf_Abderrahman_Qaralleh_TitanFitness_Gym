using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Sessions.CancelBooking;

public sealed record CancelBookingCommand(Guid SessionId, Guid BookingId) : IRequest<Guid?>;

public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.BookingId).NotEmpty();
    }
}

public sealed class CancelBookingCommandHandler(
    IClassSessionRepository sessions,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelBookingCommand, Guid?>
{
    public async Task<Guid?> Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Session", request.SessionId);

        var promoted = session.CancelBooking(request.BookingId, DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return promoted?.Id;
    }
}