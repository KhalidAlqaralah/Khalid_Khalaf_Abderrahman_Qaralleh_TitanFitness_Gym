using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Members.Queries.GetMemberActivity;

/// <summary>
/// Recent Activity: the latest check-ins and attended classes, most recent first.
/// Two joined queries (each already limited to <c>Take</c> rows) merged in memory.
/// </summary>
internal sealed class GetMemberActivityQueryHandler(
    IReadRepository<Member> members,
    IReadRepository<CheckIn> checkIns,
    IReadRepository<Branch> branches,
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IReadRepository<Trainer> trainers) : IRequestHandler<GetMemberActivityQuery, Result<IReadOnlyList<MemberActivityResponse>>>
{
    public async Task<Result<IReadOnlyList<MemberActivityResponse>>> Handle(GetMemberActivityQuery query, CancellationToken cancellationToken)
    {
        if (!await members.GetAll().AnyAsync(m => m.Id == query.MemberId, cancellationToken))
            return MemberErrors.NotFound;

        var visits = await (
            from c in checkIns.GetAll()
            join b in branches.GetAll() on c.BranchId equals b.Id
            where c.MemberId == query.MemberId
            orderby c.OccurredAt descending
            select new { b.Name, c.OccurredAt })
            .Take(query.Take)
            .ToListAsync(cancellationToken);

        var classes = await (
            from bk in bookings.GetAll()
            join s in sessions.GetAll() on bk.SessionId equals s.Id
            join t in trainers.GetAll() on s.TrainerId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            where bk.MemberId == query.MemberId && bk.Status == BookingStatus.Attended
            orderby s.Slot.Date descending, s.Slot.Start descending
            select new { s.ClassName, TrainerName = t == null ? null : t.Name, s.Slot.Date, s.Slot.Start })
            .Take(query.Take)
            .ToListAsync(cancellationToken);

        var activity = visits
            .Select(v => new MemberActivityResponse(ActivityKind.CheckIn, "Facility Check-in", $"{v.Name} Branch", v.OccurredAt))
            .Concat(classes.Select(c => new MemberActivityResponse(
                ActivityKind.ClassAttendance,
                "Class Attendance",
                c.TrainerName is null ? c.ClassName : $"{c.ClassName} with {c.TrainerName.Split(' ')[0]}",
                c.Date.ToDateTime(c.Start))))
            .OrderByDescending(a => a.OccurredAt)
            .Take(query.Take)
            .ToList();

        return activity;
    }
}
