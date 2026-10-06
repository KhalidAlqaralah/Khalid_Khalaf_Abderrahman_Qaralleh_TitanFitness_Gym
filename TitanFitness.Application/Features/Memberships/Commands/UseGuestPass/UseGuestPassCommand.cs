using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Commands.UseGuestPass;

public sealed record UseGuestPassCommand(Guid MembershipId, Guid GuestPassId, string? GuestName) : IRequest<Result>;
