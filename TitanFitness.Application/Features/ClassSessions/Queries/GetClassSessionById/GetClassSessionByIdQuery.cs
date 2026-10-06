using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetClassSessionById;

public sealed record GetClassSessionByIdQuery(Guid SessionId) : IRequest<Result<ClassSessionResponse>>;
