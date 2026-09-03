using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Infrastructure.Persistence;

public sealed class ReadQueries(TitanFitnessDbContext context) : IReadQueries
{
    public async Task<PagedResult<MemberListItem>> SearchMembersAsync(
        string? search, Guid? branchId, int page, int pageSize, DateOnly today, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        MembershipNumber? exactNumber = null;
        if (term is { Length: <= 10 })
        {
            try { exactNumber = new MembershipNumber(term); }
            catch { /* not a valid number, name search only */ }
        }

        var query = context.Members.AsNoTracking();

        if (branchId is not null)
            query = query.Where(m => m.HomeBranchId == branchId);

        if (term is not null)
            query = query.Where(m => m.FullName.Contains(term)
                                  || (exactNumber != null && m.Number == exactNumber));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(m => m.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Number,
                m.FullName,
                BranchName = context.Branches
                    .Where(b => b.Id == m.HomeBranchId)
                    .Select(b => b.Name)
                    .FirstOrDefault(),
                Status = context.Memberships
                    .Where(x => x.MemberId == m.Id
                             && x.Status != MembershipStatus.Cancelled
                             && x.Period.Start <= today
                             && today <= x.Period.End)
                    .OrderByDescending(x => x.PurchasedOn)
                    .Select(x => (MembershipStatus?)x.Status)
                    .FirstOrDefault(),
                LastVisit = context.CheckIns
                    .Where(c => c.MemberId == m.Id && c.Result == CheckInResult.Admitted)
                    .Max(c => (DateTime?)c.OccurredAt)
            })
            .ToListAsync(ct);

        var mapped = items
            .Select(x => new MemberListItem(
                x.Id, x.Number.Value, x.FullName,
                x.Status?.ToString() ?? "None",
                x.BranchName ?? "—",
                x.LastVisit))
            .ToList();

        return new PagedResult<MemberListItem>(mapped, page, pageSize, total);
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(
        Guid? branchId, DateTime now, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var from = today.ToDateTime(TimeOnly.MinValue);
        var to = from.AddDays(1);
        var soon = today.AddDays(7);

        var memberships = context.Memberships.AsNoTracking()
            .Where(m => m.Status != MembershipStatus.Cancelled
                     && m.Period.Start <= today
                     && today <= m.Period.End);

        if (branchId is not null)
            memberships = memberships.Where(m =>
                context.Members.Any(x => x.Id == m.MemberId && x.HomeBranchId == branchId));

        var active = await memberships.CountAsync(m => m.Status == MembershipStatus.Active, ct);
        var frozen = await memberships.CountAsync(m => m.Status == MembershipStatus.Frozen, ct);
        var expiring = await memberships.CountAsync(m => m.Period.End <= soon, ct);

        var checkIns = context.CheckIns.AsNoTracking()
            .Where(c => c.OccurredAt >= from && c.OccurredAt < to);

        if (branchId is not null)
            checkIns = checkIns.Where(c => c.BranchId == branchId);

        var admitted = await checkIns.CountAsync(c => c.Result == CheckInResult.Admitted, ct);
        var refused = await checkIns.CountAsync(c => c.Result == CheckInResult.Refused, ct);

        var inside = await checkIns
            .Where(c => c.Result == CheckInResult.Admitted)
            .Select(c => c.MemberId)
            .Distinct()
            .CountAsync(ct);

        var sessions = context.ClassSessions.AsNoTracking()
            .Where(s => s.Slot.Date == today && s.Status != SessionStatus.Cancelled);

        if (branchId is not null)
            sessions = sessions.Where(s => s.BranchId == branchId);

        var todaySessions = await sessions
            .Select(s => new
            {
                s.CapacityLimit,
                Confirmed = s.Bookings.Count(b => b.Status == BookingStatus.Confirmed
                                              || b.Status == BookingStatus.Attended
                                              || b.Status == BookingStatus.NoShow),
                Booked = s.Bookings.Count(b => b.Status != BookingStatus.Cancelled)
            })
            .ToListAsync(ct);

        var fillRate = todaySessions.Count == 0
            ? 0m
            : Math.Round(todaySessions.Average(s =>
                s.CapacityLimit == 0 ? 0m : (decimal)s.Confirmed / s.CapacityLimit), 4);

        return new DashboardSnapshot(
            active, inside, frozen, expiring,
            admitted, refused, todaySessions.Count,
            todaySessions.Sum(s => s.Booked), fillRate);
    }

    public async Task<IReadOnlyList<ScheduleItem>> GetScheduleAsync(
        Guid? branchId, DateOnly date, DateTime now, CancellationToken ct = default)
    {
        var query = context.ClassSessions.AsNoTracking()
            .Include(s => s.Bookings)
            .Where(s => s.Slot.Date == date);

        if (branchId is not null)
            query = query.Where(s => s.BranchId == branchId);

        var sessions = await query.ToListAsync(ct);

        var trainerNames = await context.Trainers.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var studioNames = await context.Set<Studio>().AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        return sessions
            .OrderBy(s => s.Slot.Start)
            .Select(s =>
            {
                s.RefreshStatus(now);

                return new ScheduleItem(
                    s.Id, s.ClassName, s.Slot.Date, s.Slot.Start, s.Slot.DurationInMinutes,
                    trainerNames.GetValueOrDefault(s.TrainerId, "—"),
                    studioNames.GetValueOrDefault(s.StudioId, "—"),
                    s.CapacityLimit, s.ConfirmedCount, s.WaitlistCount, s.Status.ToString());
            })
            .ToList();
    }

    public async Task<MemberProfile?> GetMemberProfileAsync(
        Guid memberId, DateOnly today, CancellationToken ct = default)
    {
        var member = await context.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == memberId, ct);

        if (member is null) return null;

        var branchName = await context.Branches.AsNoTracking()
            .Where(b => b.Id == member.HomeBranchId)
            .Select(b => b.Name)
            .FirstOrDefaultAsync(ct);

        var membership = await context.Memberships.AsNoTracking()
            .Include(m => m.Freezes)
            .Include(m => m.GuestPasses)
            .Where(m => m.MemberId == memberId
                     && m.Status != MembershipStatus.Cancelled
                     && m.Period.Start <= today
                     && today <= m.Period.End)
            .OrderByDescending(m => m.PurchasedOn)
            .FirstOrDefaultAsync(ct);

        string? planName = null;

        if (membership is not null)
        {
            membership.RefreshStatus(today);

            planName = await context.Plans.AsNoTracking()
                .Where(p => p.Id == membership.PlanId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct);
        }

        var checkIns = await context.CheckIns.AsNoTracking()
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.OccurredAt)
            .Take(7)
            .Select(c => new { c.OccurredAt, c.Result, c.RefusalReason })
            .ToListAsync(ct);

        var classes = await context.ClassSessions.AsNoTracking()
            .Where(s => s.Bookings.Any(b => b.MemberId == memberId && b.Status != BookingStatus.Cancelled))
            .OrderByDescending(s => s.Slot.Date)
            .Take(7)
            .Select(s => new
            {
                s.ClassName,
                s.Slot.Date,
                s.Slot.Start,
                Status = s.Bookings
                    .Where(b => b.MemberId == memberId && b.Status != BookingStatus.Cancelled)
                    .Select(b => b.Status)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var activity = checkIns
            .Select(c => new ActivityItem(
                c.OccurredAt,
                "Check-in",
                c.Result == CheckInResult.Admitted ? "Admitted" : $"Refused — {c.RefusalReason}"))
            .Concat(classes.Select(s => new ActivityItem(
                s.Date.ToDateTime(s.Start), "Class", $"{s.ClassName} — {s.Status}")))
            .OrderByDescending(a => a.OccurredAt)
            .Take(7)
            .ToList();

        return new MemberProfile(
            member.Id, member.Number.Value, member.FullName, member.Email, member.Phone,
            member.Address, member.PhotoUrl, member.JoinedOn, member.HomeBranchId, branchName ?? "—",
            membership?.Status.ToString() ?? "None",
            membership?.Id, planName, membership?.Terms.Price.Amount,
            membership?.Period.Start, membership?.Period.End,
            membership?.Freezes.Count ?? 0, membership?.Terms.MaxFreezes ?? 0,
            membership?.FreezeDaysUsed ?? 0, membership?.Terms.MaxFreezeDays ?? 0,
            membership?.GuestPasses.Count ?? 0, membership?.Terms.GuestPassQuota ?? 0,
            activity);
    }

    public async Task<IReadOnlyList<BranchListItem>> ListBranchesAsync(CancellationToken ct = default) =>
        await context.Branches.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BranchListItem(
                b.Id, b.Name, b.Address, b.Hours.Opens, b.Hours.Closes, b.Studios.Count))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TrainerListItem>> ListTrainersAsync(
        bool activeOnly, CancellationToken ct = default)
    {
        var query = context.Trainers.AsNoTracking();

        if (activeOnly)
            query = query.Where(t => t.IsActive);

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TrainerListItem(t.Id, t.Name, t.Email, t.Phone, t.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlanListItem>> ListPlansAsync(
        bool publishedOnly, CancellationToken ct = default)
    {
        var query = context.Plans.AsNoTracking();

        if (publishedOnly)
            query = query.Where(p => p.IsPublished);

        var plans = await query.OrderBy(p => p.Name).ToListAsync(ct);

        return plans.Select(p => new PlanListItem(
            p.Id, p.Name, p.Terms.Price.Amount, p.Terms.DurationInMonths,
            p.Terms.MaxFreezeDays, p.Terms.MaxFreezes, p.Terms.GuestPassQuota,
            p.Terms.AccessScope.ToString(), p.IsPublished)).ToList();
    }

    public async Task<SessionDetail?> GetSessionAsync(
        Guid sessionId, DateTime now, CancellationToken ct = default)
    {
        var session = await context.ClassSessions.AsNoTracking()
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null) return null;

        session.RefreshStatus(now);

        var branchName = await context.Branches.AsNoTracking()
            .Where(b => b.Id == session.BranchId).Select(b => b.Name).FirstOrDefaultAsync(ct);

        var studioName = await context.Set<Studio>().AsNoTracking()
            .Where(s => s.Id == session.StudioId).Select(s => s.Name).FirstOrDefaultAsync(ct);

        var trainerName = await context.Trainers.AsNoTracking()
            .Where(t => t.Id == session.TrainerId).Select(t => t.Name).FirstOrDefaultAsync(ct);

        var memberIds = session.Bookings.Select(b => b.MemberId).Distinct().ToList();

        var members = await context.Members.AsNoTracking()
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, m.FullName, m.Number })
            .ToListAsync(ct);

        var bookings = session.Bookings
            .OrderBy(b => b.Position)
            .Select(b =>
            {
                var m = members.FirstOrDefault(x => x.Id == b.MemberId);
                return new SessionBooking(
                    b.Id, b.MemberId,
                    m?.FullName ?? "—",
                    m?.Number.Value ?? "—",
                    b.Status.ToString(), b.Position, b.Note, b.BookedOn);
            })
            .ToList();

        return new SessionDetail(
            session.Id, session.ClassName, session.Description,
            session.BranchId, branchName ?? "—",
            session.StudioId, studioName ?? "—",
            session.TrainerId, trainerName ?? "—",
            session.Slot.Date, session.Slot.Start, session.Slot.End, session.Slot.DurationInMinutes,
            session.CapacityLimit, session.ConfirmedCount, session.WaitlistCount,
            session.RemainingPlaces, session.AttendedCount, session.NoShowCount,
            session.FillRate, session.Status.ToString(),
            bookings);
    }
}