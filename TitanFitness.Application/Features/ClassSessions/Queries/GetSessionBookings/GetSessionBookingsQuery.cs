using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetSessionBookings;

public sealed record GetSessionBookingsQuery(Guid SessionId) : IRequest<Result<IReadOnlyList<SessionBookingResponse>>>;
