using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.ClassSessions.Shared;

/// <summary>
/// The class row query shared by the schedule, the dashboard and the class dialogs: sessions joined to branch,
/// optional trainer and studio, and one grouped table of booking counts (no per-row subqueries).
/// The grouped columns are nullable because the LEFT JOIN gives NULL for a class with no bookings yet.
/// </summary>
internal static class ClassSessionRows
{
    public static IQueryable<ClassSessionResponse> Query(
        IQueryable<ClassSession> sessions,
        IQueryable<Booking> bookings,
        IQueryable<Branch> branches,
        IQueryable<Trainer> trainers,
        IQueryable<Studio> studios)
    {
        var counts =
            from b in bookings
            group b by b.SessionId into g
            select new
            {
                SessionId = (Guid?)g.Key,
                Enrolled = (int?)g.Sum(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Attended || b.Status == BookingStatus.NoShow ? 1 : 0),
                Waitlist = (int?)g.Sum(b => b.Status == BookingStatus.Waitlisted ? 1 : 0),
                Active = (int?)g.Sum(b => b.Status != BookingStatus.Cancelled ? 1 : 0)
            };

        return
            from s in sessions
            join br in branches on s.BranchId equals br.Id
            join t in trainers on s.TrainerId equals (Guid?)t.Id into tj
            from t in tj.DefaultIfEmpty()
            join st in studios on s.StudioId equals (Guid?)st.Id into sj
            from st in sj.DefaultIfEmpty()
            join c in counts on (Guid?)s.Id equals c.SessionId into cj
            from c in cj.DefaultIfEmpty()
            select new ClassSessionResponse
            {
                Id = s.Id,
                ClassName = s.ClassName,
                Description = s.Description,
                BranchId = s.BranchId,
                BranchName = br.Name,
                TrainerId = s.TrainerId,
                TrainerName = t == null ? null : t.Name,
                StudioId = s.StudioId,
                StudioName = st == null ? null : st.Name,
                StudioCapacity = st == null ? null : st.Capacity,
                Date = s.Slot.Date,
                StartTime = s.Slot.Start,
                DurationInMinutes = s.Slot.DurationInMinutes,
                CapacityLimit = s.CapacityLimit,
                Enrolled = c == null ? 0 : c.Enrolled ?? 0,
                Waitlist = c == null ? 0 : c.Waitlist ?? 0,
                ActiveBookings = c == null ? 0 : c.Active ?? 0,
                IsCancelled = s.CancelledAt != null
            };
    }

    /// <summary>Adds the display state, which depends on the clock, after the rows are read.</summary>
    public static ClassSessionResponse WithState(this ClassSessionResponse row, DateTime now)
    {
        var starts = row.Date.ToDateTime(row.StartTime);
        return row with
        {
            State = ClassSession.StateOf(row.IsCancelled, starts, starts.AddMinutes(row.DurationInMinutes), row.Enrolled, row.CapacityLimit, now)
        };
    }
}
