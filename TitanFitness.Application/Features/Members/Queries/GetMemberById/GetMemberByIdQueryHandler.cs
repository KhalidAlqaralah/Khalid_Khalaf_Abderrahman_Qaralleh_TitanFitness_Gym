using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Members.Queries.GetMemberById;

/// <summary>The identity card on the Member Profile.</summary>
internal sealed class GetMemberByIdQueryHandler(
    IReadRepository<Member> members,
    IReadRepository<Branch> branches,
    IReadRepository<Membership> memberships,
    IReadRepository<CheckIn> checkIns,
    IClock clock) : IRequestHandler<GetMemberByIdQuery, Result<MemberResponse>>
{
    public async Task<Result<MemberResponse>> Handle(GetMemberByIdQuery query, CancellationToken cancellationToken)
    {
        var member = await (
            from m in members.GetAll()
            join b in branches.GetAll() on m.HomeBranchId equals b.Id
            where m.Id == query.MemberId
            select new MemberResponse
            {
                Id = m.Id,
                MembershipNumber = m.Number.Value,
                FullName = m.FullName,
                Email = m.Email == null ? null : m.Email.Value,
                Phone = m.Phone,
                Address = m.Address,
                JoinedOn = m.JoinedOn,
                PhotoUrl = m.PhotoUrl,
                HomeBranchId = m.HomeBranchId,
                HomeBranchName = b.Name,
                CreatedBy = m.CreatedBy,
                CreatedAt = m.CreatedAt
            }).FirstOrDefaultAsync(cancellationToken);

        if (member is null)
            return MemberErrors.NotFound;

        var held = await memberships.GetAll()
            .Where(x => x.MemberId == query.MemberId)
            .ToListAsync(cancellationToken);

        var lastVisit = await checkIns.GetAll()
            .Where(c => c.MemberId == query.MemberId)
            .MaxAsync(c => (DateTime?)c.OccurredAt, cancellationToken);

        var today = clock.Today;
        var status = MembershipSelection.StatusOf(
            MembershipSelection.Current(held, today), held.Any(x => x.CancelledOn is not null), today);

        return member with { Status = status, LastVisit = lastVisit };
    }
}
