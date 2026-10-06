using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Commands.EndFreezeEarly;

public sealed record EndFreezeEarlyCommand(Guid MembershipId, Guid FreezeId, DateOnly EndedOn) : IRequest<Result>;
