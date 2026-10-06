using MediatR;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Memberships.Commands.IssueGuestPass;

public sealed record IssueGuestPassCommand(Guid MembershipId) : IRequest<Result<GuestPassIssuedResponse>>;
