using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Application.Features.Memberships.Shared;

namespace TitanFitness.Application.Features.Members.Queries.GetMembers;

public sealed record GetMembersQuery(
    int Page,
    int PageSize,
    string? Search,
    string? SortBy,
    SortDirection SortDirection,
    Guid? BranchId,
    IReadOnlyList<MemberStatus> Statuses) : IRequest<PagedResult<MemberListItemResponse>>;
