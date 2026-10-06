using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Members.Queries.GetMembers;

/// <summary>
/// Member Directory. One SQL statement: members joined to their branch and to small grouped tables
/// (membership summary, freeze stats, last visit) instead of a subquery per row.
/// The current membership follows <see cref="MembershipSelection.Current"/>: latest started, not cancelled.
/// </summary>
internal sealed class GetMembersQueryHandler(
    IReadRepository<Member> members,
    IReadRepository<Branch> branches,
    IReadRepository<Membership> memberships,
    IReadRepository<Freeze> freezes,
    IReadRepository<CheckIn> checkIns,
    IClock clock) : IRequestHandler<GetMembersQuery, PagedResult<MemberListItemResponse>>
{
    public Task<PagedResult<MemberListItemResponse>> Handle(GetMembersQuery query, CancellationToken cancellationToken)
    {
        var today = clock.Today;

        var source = members.GetAll();
        if (query.BranchId is not null)
            source = source.Where(m => m.HomeBranchId == query.BranchId);

        // Small grouped tables, each filtered first so the GROUP BY has only plain aggregates.
        // Per member: start of the latest live membership that has started.
        var latestStarts =
            from x in memberships.GetAll()
            where x.CancelledOn == null && x.Period.Start <= today
            group x.Period.Start by x.MemberId into g
            select new { MemberId = g.Key, Start = g.Max() };

        // Members with a live membership still to start / with a cancelled membership.
        var upcoming =
            from x in memberships.GetAll()
            where x.CancelledOn == null && x.Period.Start > today
            group x by x.MemberId into g
            select (Guid?)g.Key;

        var cancelled =
            from x in memberships.GetAll()
            where x.CancelledOn != null
            group x by x.MemberId into g
            select (Guid?)g.Key;

        // Per membership: freezes taken, and the memberships frozen today.
        var freezeCounts =
            from f in freezes.GetAll()
            group f by f.MembershipId into g
            select new { MembershipId = g.Key, Count = g.Count() };

        var frozenToday =
            from f in freezes.GetAll()
            where f.Period.Start <= today && (f.EndedEarlyOn ?? f.Period.End) >= today
            group f by f.MembershipId into g
            select (Guid?)g.Key;

        // Per member: last check-in.
        var visits =
            from c in checkIns.GetAll()
            group c by c.MemberId into g
            select new { MemberId = g.Key, Last = g.Max(c => c.OccurredAt) };

        var live = memberships.GetAll().Where(x => x.CancelledOn == null);

        // Left-joined values are read as nullable scalars, then the status is worked out in a second Select
        // (both Selects become one SQL statement with CASE expressions).
        var joined =
            from m in source
            join b in branches.GetAll() on m.HomeBranchId equals b.Id
            join ls in latestStarts on m.Id equals ls.MemberId into lsj
            from ls in lsj.DefaultIfEmpty()
            join ms in live on new { MemberId = m.Id, Start = (DateOnly?)ls.Start } equals new { ms.MemberId, Start = (DateOnly?)ms.Period.Start } into msj
            from ms in msj.DefaultIfEmpty()
            join fc in freezeCounts on ms.Id equals fc.MembershipId into fcj
            from fc in fcj.DefaultIfEmpty()
            join fz in frozenToday on (Guid?)ms.Id equals fz into fzj
            from fz in fzj.DefaultIfEmpty()
            join up in upcoming on (Guid?)m.Id equals up into upj
            from up in upj.DefaultIfEmpty()
            join cx in cancelled on (Guid?)m.Id equals cx into cxj
            from cx in cxj.DefaultIfEmpty()
            join v in visits on m.Id equals v.MemberId into vj
            from v in vj.DefaultIfEmpty()
            select new
            {
                m.Id,
                Number = m.Number.Value,
                m.FullName,
                m.PhotoUrl,
                m.HomeBranchId,
                BranchName = b.Name,
                LastVisit = (DateTime?)v.Last,
                CurrentEnd = (DateOnly?)ms.Period.End,
                MaxFreezes = (int?)ms.Terms.MaxFreezes,
                FreezeCount = (int?)fc.Count,
                IsFrozen = fz != null,
                HasUpcoming = up != null,
                HasCancelled = cx != null
            };

        var rows = joined.Select(x => new MemberListItemResponse
        {
            Id = x.Id,
            MembershipNumber = x.Number,
            FullName = x.FullName,
            PhotoUrl = x.PhotoUrl,
            BranchId = x.HomeBranchId,
            BranchName = x.BranchName,
            LastVisit = x.LastVisit,
            Status = x.CurrentEnd != null
                ? (x.CurrentEnd < today ? MemberStatus.Expired
                    : x.IsFrozen ? MemberStatus.Frozen
                    : MemberStatus.Active)
                : x.HasUpcoming ? MemberStatus.Pending
                : x.HasCancelled ? MemberStatus.Cancelled
                : MemberStatus.None,
            RemainingFreezes = x.CurrentEnd == null ? 0 : (x.MaxFreezes ?? 0) - (x.FreezeCount ?? 0)
        });

        if (query.Statuses.Count > 0)
            rows = rows.Where(r => query.Statuses.Contains(r.Status));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().TrimStart('#');
            var matchingStatuses = Enum.GetValues<MemberStatus>()
                .Where(s => s.ToString().StartsWith(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            rows = rows.Where(r => r.FullName.Contains(term)
                                || r.MembershipNumber.Contains(term)
                                || r.BranchName.Contains(term)
                                || matchingStatuses.Contains(r.Status));
        }

        var ordered = (query.SortBy?.ToLowerInvariant()) switch
        {
            "number" => rows.OrderByDirection(r => r.MembershipNumber, query.SortDirection),
            "status" => rows.OrderByDirection(r => r.Status, query.SortDirection),
            "branch" => rows.OrderByDirection(r => r.BranchName, query.SortDirection),
            "lastvisit" => rows.OrderByDirection(r => r.LastVisit, query.SortDirection),
            _ => rows.OrderByDirection(r => r.FullName, query.SortDirection)
        };

        return ordered.ThenBy(r => r.MembershipNumber).ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
