using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Sessions.MarkAttendance;

public sealed record MarkAttendanceCommand(Guid SessionId, Guid BookingId, bool Attended) : IRequest<Unit>;

public sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.BookingId).NotEmpty();
    }
}

public sealed class MarkAttendanceCommandHandler(
    IClassSessionRepository sessions,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkAttendanceCommand, Unit>
{
    public async Task<Unit> Handle(MarkAttendanceCommand request, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Session", request.SessionId);

        session.MarkAttendance(request.BookingId, request.Attended, DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}