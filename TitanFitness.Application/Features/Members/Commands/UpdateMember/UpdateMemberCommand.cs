using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Members.Commands.UpdateMember;

public sealed record UpdateMemberCommand(Guid MemberId, string FullName, Guid HomeBranchId) : IRequest<Result>;
