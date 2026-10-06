using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.CancelBooking;

public sealed record CancelBookingCommand(Guid SessionId, Guid BookingId) : IRequest<Result<BookingCancelledResponse>>;
