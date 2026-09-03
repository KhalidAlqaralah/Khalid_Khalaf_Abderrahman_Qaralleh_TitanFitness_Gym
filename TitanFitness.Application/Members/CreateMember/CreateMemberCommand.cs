using MediatR;

namespace TitanFitness.Application.Members.CreateMember;

public sealed record CreateMemberCommand(
    string MembershipNumber,
    string FullName,
    string? Email,
    string? Phone,
    string? Address,
    DateOnly JoinedOn,
    Guid HomeBranchId) : IRequest<Guid>;