using MediatR;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Members.Commands.CreateMember;

public sealed record CreateMemberCommand(string FullName, Guid HomeBranchId, string? Email, string? Phone, string? Address)
    : IRequest<Result<MemberCreatedResponse>>;
