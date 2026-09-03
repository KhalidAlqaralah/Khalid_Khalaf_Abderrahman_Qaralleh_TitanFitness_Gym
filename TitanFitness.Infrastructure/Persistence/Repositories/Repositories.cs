using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository(TitanFitnessDbContext context) : IBranchRepository
{
    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Branches
               .Include(b => b.Studios)
               .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct = default) =>
        await context.Branches
                     .Include(b => b.Studios)
                     .OrderBy(b => b.Name)
                     .ToListAsync(ct);

    public void Add(Branch branch) => context.Branches.Add(branch);
}

public sealed class MemberRepository(TitanFitnessDbContext context) : IMemberRepository
{
    public Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Members.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<Member?> GetByNumberAsync(MembershipNumber number, CancellationToken ct = default) =>
        context.Members.FirstOrDefaultAsync(m => m.Number == number, ct);

    public Task<bool> NumberExistsAsync(MembershipNumber number, CancellationToken ct = default) =>
        context.Members.AnyAsync(m => m.Number == number, ct);

    public void Add(Member member) => context.Members.Add(member);
}

public sealed class PlanRepository(TitanFitnessDbContext context) : IPlanRepository
{
    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Plan>> ListPublishedAsync(CancellationToken ct = default) =>
        await context.Plans
                     .Where(p => p.IsPublished)
                     .OrderBy(p => p.Name)
                     .ToListAsync(ct);

    public void Add(Plan plan) => context.Plans.Add(plan);
}

public sealed class MembershipRepository(TitanFitnessDbContext context) : IMembershipRepository
{
    public Task<Membership?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Memberships
               .Include(m => m.Freezes)
               .Include(m => m.GuestPasses)
               .FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<Membership?> GetActiveForMemberAsync(Guid memberId, DateOnly on, CancellationToken ct = default) =>
        context.Memberships
               .Include(m => m.Freezes)
               .Include(m => m.GuestPasses)
               .FirstOrDefaultAsync(m =>
                   m.MemberId == memberId &&
                   m.Status != MembershipStatus.Cancelled &&
                   m.Period.Start <= on &&
                   on <= m.Period.End, ct);

    public Task<bool> HasOverlappingMembershipAsync(Guid memberId, DateRange period, CancellationToken ct = default)
    {
        var start = period.Start;
        var end = period.End;

        return context.Memberships.AnyAsync(m =>
            m.MemberId == memberId &&
            m.Status != MembershipStatus.Cancelled &&
            m.Period.Start <= end &&
            start <= m.Period.End, ct);
    }

    public void Add(Membership membership) => context.Memberships.Add(membership);
}

public sealed class TrainerRepository(TitanFitnessDbContext context) : ITrainerRepository
{
    public Task<Trainer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Trainers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Trainer>> ListActiveAsync(CancellationToken ct = default) =>
        await context.Trainers
                     .Where(t => t.IsActive)
                     .OrderBy(t => t.Name)
                     .ToListAsync(ct);

    public void Add(Trainer trainer) => context.Trainers.Add(trainer);
}

public sealed class ClassSessionRepository(TitanFitnessDbContext context) : IClassSessionRepository
{
    public Task<ClassSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.ClassSessions
               .Include(s => s.Bookings)
               .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<bool> TrainerIsBusyAsync(
        Guid trainerId, TimeSlot slot, Guid? excludingSessionId = null, CancellationToken ct = default)
    {
        var sameDay = await context.ClassSessions
            .Where(s => s.TrainerId == trainerId
                     && s.Slot.Date == slot.Date
                     && s.Status != SessionStatus.Cancelled
                     && (excludingSessionId == null || s.Id != excludingSessionId))
            .Select(s => new { s.Slot.Date, s.Slot.Start, s.Slot.DurationInMinutes })
            .ToListAsync(ct);

        return sameDay.Any(x => new TimeSlot(x.Date, x.Start, x.DurationInMinutes).Overlaps(slot));
    }

    public async Task<bool> StudioIsBusyAsync(
        Guid studioId, TimeSlot slot, Guid? excludingSessionId = null, CancellationToken ct = default)
    {
        var sameDay = await context.ClassSessions
            .Where(s => s.StudioId == studioId
                     && s.Slot.Date == slot.Date
                     && s.Status != SessionStatus.Cancelled
                     && (excludingSessionId == null || s.Id != excludingSessionId))
            .Select(s => new { s.Slot.Date, s.Slot.Start, s.Slot.DurationInMinutes })
            .ToListAsync(ct);

        return sameDay.Any(x => new TimeSlot(x.Date, x.Start, x.DurationInMinutes).Overlaps(slot));
    }

    public async Task<bool> MemberHasOverlappingBookingAsync(
        Guid memberId, TimeSlot slot, CancellationToken ct = default)
    {
        var sameDay = await context.ClassSessions
            .Where(s => s.Slot.Date == slot.Date
                     && s.Status != SessionStatus.Cancelled
                     && s.Bookings.Any(b => b.MemberId == memberId && b.Status != BookingStatus.Cancelled))
            .Select(s => new { s.Slot.Date, s.Slot.Start, s.Slot.DurationInMinutes })
            .ToListAsync(ct);

        return sameDay.Any(x => new TimeSlot(x.Date, x.Start, x.DurationInMinutes).Overlaps(slot));
    }

    public void Add(ClassSession session) => context.ClassSessions.Add(session);
}

public sealed class CheckInRepository(TitanFitnessDbContext context) : ICheckInRepository
{
    public Task<int> CountAdmittedOnAsync(Guid? branchId, DateOnly date, CancellationToken ct = default)
    {
        var from = date.ToDateTime(TimeOnly.MinValue);
        var to = from.AddDays(1);

        return context.CheckIns.CountAsync(c =>
            c.Result == CheckInResult.Admitted &&
            c.OccurredAt >= from &&
            c.OccurredAt < to &&
            (branchId == null || c.BranchId == branchId), ct);
    }

    public void Add(CheckIn checkIn) => context.CheckIns.Add(checkIn);
}