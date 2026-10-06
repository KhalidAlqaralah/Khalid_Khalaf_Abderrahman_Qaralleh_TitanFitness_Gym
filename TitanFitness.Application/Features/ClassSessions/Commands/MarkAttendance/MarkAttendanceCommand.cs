using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.MarkAttendance;

public sealed record MarkAttendanceCommand(Guid SessionId, Guid BookingId, bool Attended) : IRequest<Result>;
