using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.BookSession;

/// <summary>Used by staff (Book Session) and by a member booking for themselves (Member View).</summary>
public sealed record BookSessionCommand(Guid SessionId, Guid MemberId, string? Note) : IRequest<Result<BookingResultResponse>>;
