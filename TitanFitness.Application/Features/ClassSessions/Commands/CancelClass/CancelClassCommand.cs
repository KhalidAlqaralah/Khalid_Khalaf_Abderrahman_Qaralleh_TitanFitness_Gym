using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.CancelClass;

public sealed record CancelClassCommand(Guid SessionId) : IRequest<Result>;
