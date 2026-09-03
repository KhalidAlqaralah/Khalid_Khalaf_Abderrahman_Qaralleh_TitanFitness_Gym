using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Members.SearchMembers;

public sealed record SearchMembersQuery(
    string? Search, Guid? BranchId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<MemberListItem>>;

public sealed class SearchMembersQueryHandler(IReadQueries queries)
    : IRequestHandler<SearchMembersQuery, PagedResult<MemberListItem>>
{
    public Task<PagedResult<MemberListItem>> Handle(SearchMembersQuery request, CancellationToken ct) =>
        queries.SearchMembersAsync(
            request.Search, request.BranchId, request.Page, request.PageSize,
            DateOnly.FromDateTime(DateTime.Now), ct);
}