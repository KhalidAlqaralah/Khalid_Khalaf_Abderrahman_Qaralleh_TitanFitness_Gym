using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Infrastructure.Persistence.Seeding;

/// <summary>
/// Fills an empty database with demo data built through the domain factories, relative to today,
/// so every screen has something to show and every rule has a member to try it on.
/// Runs only when there are no branches yet.
/// </summary>
public sealed class DatabaseSeeder(TitanFitnessDbContext context, IClock clock, ILogger<DatabaseSeeder> logger)
{
    private const string Seed = "seed";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Branches.AnyAsync(cancellationToken))
            return;

        var clockNow = clock.Now;
        var now = new DateTime(clockNow.Year, clockNow.Month, clockNow.Day, clockNow.Hour, clockNow.Minute, 0);
        var today = DateOnly.FromDateTime(now);

        // ---------- branches & studios ----------
        var downtown = Branch.Create("Downtown", "12 King Hussein St, Amman", Hours(6, 23)).Value;
        var uptown = Branch.Create("Uptown", "48 Mecca St, Amman", Hours(6, 22)).Value;

        var studioA = downtown.AddStudio("Studio A", 30).Value;
        var studioB = downtown.AddStudio("Studio B", 25).Value;
        var zenRoom = downtown.AddStudio("Zen Room", 20).Value;
        var cycle = downtown.AddStudio("Cycle Studio", 40).Value;
        var upStudio = uptown.AddStudio("Studio A", 30).Value;
        var spinRoom = uptown.AddStudio("Spin Room", 35).Value;
        var yogaLoft = uptown.AddStudio("Yoga Loft", 20).Value;

        context.Branches.AddRange(downtown, uptown);

        // ---------- plans ----------
        Plan NewPlan(string name, decimal price, int months, int freezeDays, int freezes, int guests, AccessScope scope, bool published) =>
            Plan.Create(name, MembershipTerms.Create(Money.Create(price).Value, months, freezeDays, freezes, guests, scope).Value, published).Value;

        var annualPro = NewPlan("Annual Pro", 899m, 12, 60, 3, 5, AccessScope.AllBranches, true);
        var quarterly = NewPlan("Quarterly", 279m, 3, 15, 1, 2, AccessScope.AllBranches, true);
        var monthly = NewPlan("Monthly", 99m, 1, 0, 0, 0, AccessScope.HomeBranchOnly, true);
        var student = NewPlan("Student 2024", 69m, 1, 0, 0, 0, AccessScope.HomeBranchOnly, false);
        var semiAnnual = NewPlan("Semi-Annual Plus", 499m, 6, 30, 2, 3, AccessScope.AllBranches, true);
        var weekend = NewPlan("Weekend Warrior", 59m, 1, 0, 0, 1, AccessScope.HomeBranchOnly, true);
        var family = NewPlan("Family Annual", 1499m, 12, 90, 3, 10, AccessScope.AllBranches, true);
        var corporate = NewPlan("Corporate Quarterly", 249m, 3, 15, 1, 1, AccessScope.AllBranches, true);
        var senior = NewPlan("Senior Monthly", 49m, 1, 0, 0, 0, AccessScope.HomeBranchOnly, true);
        var premiumMonthly = NewPlan("Premium Monthly", 149m, 1, 7, 1, 1, AccessScope.AllBranches, true);
        var elite = NewPlan("Two-Year Elite", 1599m, 24, 120, 4, 12, AccessScope.AllBranches, true);
        var summer = NewPlan("Summer Pass 2025", 199m, 3, 0, 0, 0, AccessScope.HomeBranchOnly, false);

        context.Plans.AddRange(annualPro, quarterly, monthly, student, semiAnnual, weekend, family, corporate, senior, premiumMonthly, elite, summer);

        // ---------- trainers ----------
        Trainer NewTrainer(int code, string name, string specialty, Branch branch, bool active, string phone) =>
            Trainer.Create(Trainer.CodeFor(code), name, specialty, branch.Id,
                $"{name.Split(' ')[0][0]}.{name.Split(' ')[1]}@titanfitness.com".ToLowerInvariant(), phone, active, Seed, now).Value;

        var sarah = NewTrainer(1042, "Sarah Jenkins", "HIIT / Strength", downtown, true, "+1 (555) 019-4471");
        var marcusLee = NewTrainer(1057, "Marcus Lee", "Yoga / Mobility", downtown, true, "+1 (555) 019-4472");
        var elena = NewTrainer(1063, "Elena Rodriguez", "Cycling", uptown, true, "+1 (555) 019-4473");
        var mike = NewTrainer(1071, "Mike Turner", "Pilates", uptown, false, "+1 (555) 019-4474");
        var david = NewTrainer(1078, "David Kim", "Boxing", downtown, true, "+1 (555) 019-4475");
        var priya = NewTrainer(1083, "Priya Patel", "Yoga / Mobility", uptown, true, "+1 (555) 019-4476");
        var omar = NewTrainer(1089, "Omar Haddad", "HIIT / Strength", uptown, true, "+1 (555) 019-4477");
        var lina = NewTrainer(1094, "Lina Saleh", "Pilates", downtown, true, "+1 (555) 019-4478");
        var james = NewTrainer(1101, "James Carter", "CrossFit", downtown, true, "+1 (555) 019-4479");
        var nora = NewTrainer(1107, "Nora Ali", "Cycling", downtown, true, "+1 (555) 019-4480");
        var tom = NewTrainer(1112, "Tom Becker", "Strength", uptown, false, "+1 (555) 019-4481");
        var aisha = NewTrainer(1118, "Aisha Khan", "Dance Fitness", downtown, true, "+1 (555) 019-4482");

        context.Trainers.AddRange(sarah, marcusLee, elena, mike, david, priya, omar, lina, james, nora, tom, aisha);

        // ---------- members & memberships ----------
        var sequence = 1000;
        var members = new List<Member>();

        Member NewMember(string name, Branch branch, int joinedDaysAgo, bool contact = true)
        {
            sequence++;
            var first = name.Split(' ')[0].ToLowerInvariant();
            var member = Member.Create(
                MembershipNumber.FromSequence(sequence), name, branch.Id, today.AddDays(-joinedDaysAgo), Seed, now,
                contact ? $"{first}.{sequence}@email.com" : null,
                contact ? $"+1 (555) 01{sequence % 10}-{sequence:D4}" : null,
                contact ? $"{sequence % 300 + 1} West Metro Dr, Apt {sequence % 9 + 1}B" : null).Value;
            members.Add(member);
            return member;
        }

        Membership Sell(Member member, Plan plan, DateOnly start)
        {
            var membership = Membership.Purchase(member.Id, plan, start, start.ToDateTime(new TimeOnly(9, 0))).Value;
            membership.RefreshStatus(today);
            context.Memberships.Add(membership);
            return membership;
        }

        var alex = NewMember("Alex Rivera", downtown, 720);
        var alexPlan = Sell(alex, annualPro, today.AddMonths(-7));
        var firstFreezeStart = today.AddMonths(-6);
        var firstFreeze = alexPlan.ApplyFreeze(firstFreezeStart, 1, FreezeReason.ExtendedTravel, "Summer trip", firstFreezeStart.ToDateTime(new TimeOnly(8, 0))).Value;
        _ = alexPlan.EndFreezeEarly(firstFreeze.Id, firstFreezeStart.AddDays(19), firstFreezeStart.AddDays(19).ToDateTime(new TimeOnly(8, 0)));
        _ = alexPlan.ApplyFreeze(today.AddMonths(-3), 1, FreezeReason.Injury, null, today.AddMonths(-3).ToDateTime(new TimeOnly(8, 0))).Value;
        _ = alexPlan.IssueGuestPass(today.AddDays(-20).ToDateTime(new TimeOnly(10, 0))).Value;

        var jane = NewMember("Jane Doe", downtown, 400);
        Sell(jane, annualPro, today.AddMonths(-2));

        var john = NewMember("John Smith", uptown, 380);
        var johnPlan = Sell(john, family, today.AddMonths(-2));
        _ = johnPlan.ApplyFreeze(today.AddDays(-10), 1, FreezeReason.Medical, "Doctor's note on file", today.AddDays(-10).ToDateTime(new TimeOnly(8, 0))).Value;

        var alice = NewMember("Alice Williams", downtown, 300);
        Sell(alice, quarterly, today.AddDays(-30));

        var robert = NewMember("Robert Johnson", downtown, 200);
        Sell(robert, monthly, today.AddMonths(-3));

        var marcusVance = NewMember("Marcus Vance", downtown, 500);
        Sell(marcusVance, annualPro, today.AddMonths(-1));

        var noPlan = NewMember("Hannah Brooks", uptown, 3, contact: false);

        var pending = NewMember("Leo Martins", uptown, 2);
        Sell(pending, monthly, today.AddDays(5));

        var cancelled = NewMember("Grace Chen", downtown, 150);
        Sell(cancelled, quarterly, today.AddDays(-40)).Cancel(today.AddDays(-5));

        var homeOnly = NewMember("Samir Nasser", uptown, 90);
        Sell(homeOnly, monthly, today.AddDays(-10));

        string[] more =
        [
            "Olivia Brown", "Liam Wilson", "Emma Davis", "Noah Garcia", "Ava Martinez", "Ethan Anderson",
            "Sophia Thomas", "Mason Taylor", "Isabella Moore", "Lucas Jackson", "Mia White", "Henry Harris",
            "Amelia Clark", "Jack Lewis", "Harper Young", "Daniel Hall", "Evelyn Allen", "Yousef Odeh",
            "Rana Khalil", "Karim Haddad"
        ];

        var expired = new List<Member> { robert };
        Plan[] rotation = [annualPro, quarterly, monthly, semiAnnual, family, premiumMonthly, elite, corporate];
        for (var i = 0; i < more.Length; i++)
        {
            var member = NewMember(more[i], i % 3 == 0 ? uptown : downtown, 30 + i * 11);
            var plan = rotation[i % rotation.Length];
            var start = today.AddDays(-(5 + i * 3));

            // Every seventh member's plan ran out a month ago.
            if (i % 7 == 6)
            {
                start = today.AddMonths(-plan.Terms.DurationInMonths - 1);
                expired.Add(member);
            }
            else if (DateRange.ForMonths(start, plan.Terms.DurationInMonths).Value.End < today)
            {
                expired.Add(member);
            }

            Sell(member, plan, start);
        }

        context.Members.AddRange(members);

        // ---------- check-ins ----------
        void CheckInAt(Member member, Branch branch, DateTime at)
        {
            if (at > now)
                return;
            context.CheckIns.Add(CheckIn.Record(member.Id, branch.Id, at, null, Seed, at).Value);
        }

        var active = members.Where(m => !expired.Contains(m) && m != noPlan && m != pending && m != cancelled && m != john).ToList();

        for (var i = 0; i < active.Count; i++)
        {
            var member = active[i];
            var branch = member.HomeBranchId == uptown.Id ? uptown : downtown;

            if (i % 2 == 0)
                CheckInAt(member, branch, now.AddMinutes(-(10 + i * 12)));

            CheckInAt(member, branch, now.AddDays(-7).AddMinutes(-(30 + i * 9)));
            CheckInAt(member, branch, now.AddDays(-(1 + i % 5)).Date.AddHours(7 + i % 10));
        }

        CheckInAt(robert, downtown, today.AddMonths(-2).ToDateTime(new TimeOnly(8, 30)));
        foreach (var member in expired.Where(m => m != robert))
            CheckInAt(member, member.HomeBranchId == uptown.Id ? uptown : downtown, today.AddMonths(-1).AddDays(-3).ToDateTime(new TimeOnly(18, 0)));
        CheckInAt(john, uptown, today.AddDays(-12).ToDateTime(new TimeOnly(18, 15)));

        // ---------- class sessions ----------
        void Add(ClassSession session) => context.ClassSessions.Add(session);

        ClassSession Schedule(string name, Branch branch, Trainer? trainer, Studio? studio, DateTime start, int minutes, int capacity, string? description = null)
        {
            var slot = TimeSlot.Create(DateOnly.FromDateTime(start), TimeOnly.FromDateTime(start), minutes).Value;
            var session = ClassSession.Schedule(name, branch.Id, trainer?.Id, studio?.Id, slot, capacity, description, Seed, start.AddDays(-2)).Value;
            Add(session);
            return session;
        }

        void Fill(ClassSession session, int count, DateTime bookedAt, int startIndex = 0)
        {
            for (var i = 0; i < count; i++)
                session.Book(members[(startIndex + i) % members.Count].Id, null, bookedAt);
        }

        var quarter = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / 15 * 15, 0);
        DateTime SafeToday(DateTime start, int minutes) =>
            start.Date == start.AddMinutes(minutes).AddTicks(-1).Date && start.Date == now.Date ? start : DateTime.MinValue;

        // Today, Downtown: completed, in progress, full with waitlist, upcoming, cancelled.
        var completedStart = SafeToday(quarter.AddHours(-3), 60);
        if (completedStart != DateTime.MinValue)
        {
            var completed = Schedule("Morning Strength", downtown, james, studioA, completedStart, 60, 20, "Compound lifts and accessory work.");
            Fill(completed, 8, completedStart.AddDays(-1));
            foreach (var booking in completed.Bookings.Take(6).ToList())
                completed.MarkAttendance(booking.Id, true, completedStart.AddMinutes(30));
            foreach (var booking in completed.Bookings.Skip(6).ToList())
                completed.MarkAttendance(booking.Id, false, completedStart.AddMinutes(30));
        }

        var progressStart = SafeToday(quarter.AddMinutes(-15), 45);
        if (progressStart != DateTime.MinValue)
            Fill(Schedule("Vinyasa Flow Yoga", downtown, marcusLee, zenRoom, progressStart, 45, 20), 15, progressStart.AddDays(-1));

        var times = Enumerable.Range(1, 6).Select(h => quarter.AddHours(h)).ToList();
        string[] names = ["HIIT Core Blast", "Lunchtime Spin", "Power Pilates", "Boxing Basics", "Evening Stretch", "Dance Cardio"];
        Trainer[] teachers = [sarah, nora, lina, david, marcusLee, aisha];
        Studio[] rooms = [studioA, cycle, studioB, studioA, zenRoom, studioB];
        int[] caps = [30, 10, 25, 20, 20, 25];
        int[] fills = [28, 12, 9, 4, 0, 18];

        for (var i = 0; i < times.Count; i++)
        {
            var minutes = i % 3 == 0 ? 45 : i % 3 == 1 ? 60 : 30;
            var start = SafeToday(times[i], minutes);
            if (start == DateTime.MinValue)
                start = today.AddDays(1).ToDateTime(new TimeOnly(9 + i, 0));

            var session = Schedule(names[i], downtown, teachers[i], rooms[i], start, minutes, caps[i],
                i == 0 ? "Full-body intervals. Bring a towel and water." : null);
            Fill(session, fills[i], now.AddHours(-5), startIndex: i * 3);

            if (i == 4)
                session.Cancel(now.AddHours(-1));
        }

        // Today, Uptown.
        var upStart = SafeToday(quarter.AddHours(2), 60);
        if (upStart != DateTime.MinValue)
            Fill(Schedule("Rhythm Ride", uptown, elena, spinRoom, upStart, 60, 35), 20, now.AddHours(-6), 5);

        // The next seven days at both branches.
        for (var day = 1; day <= 7; day++)
        {
            var date = today.AddDays(day);
            Fill(Schedule("HIIT Core Blast", downtown, sarah, studioA, date.ToDateTime(new TimeOnly(6, 0)), 45, 30), day * 3 % 25, now.AddHours(-1), day);
            Fill(Schedule("Vinyasa Flow Yoga", downtown, marcusLee, zenRoom, date.ToDateTime(new TimeOnly(8, 0)), 60, 20), day * 2, now.AddHours(-1), day + 4);
            Fill(Schedule("Lunchtime Spin", downtown, nora, cycle, date.ToDateTime(new TimeOnly(12, 0)), 45, 40), day * 5 % 40, now.AddHours(-1), day + 8);
            Schedule("Core & Mobility", downtown, null, null, date.ToDateTime(new TimeOnly(17, 30)), 30, 20);
            Fill(Schedule("Rhythm Ride", uptown, elena, spinRoom, date.ToDateTime(new TimeOnly(7, 0)), 60, 35), day * 4, now.AddHours(-1), day + 2);
            Fill(Schedule("Sunrise Yoga", uptown, priya, yogaLoft, date.ToDateTime(new TimeOnly(6, 30)), 45, 20), day, now.AddHours(-1), day + 6);
            Schedule("Strength Circuit", uptown, omar, upStudio, date.ToDateTime(new TimeOnly(18, 0)), 60, 30);
        }

        // Past classes Alex attended, for the profile's Recent Activity.
        var yesterday = today.AddDays(-2).ToDateTime(new TimeOnly(17, 30));
        var bootcamp = Schedule("HIIT Bootcamp", downtown, sarah, studioA, yesterday, 60, 30);
        var alexBooking = bootcamp.Book(alex.Id, null, yesterday.AddDays(-1)).Value;
        bootcamp.MarkAttendance(alexBooking.Id, true, yesterday.AddMinutes(20));

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded demo data: {Members} members, {Plans} plans, {Trainers} trainers.", members.Count, 12, 12);
    }

    private static OperatingHours Hours(int opens, int closes) =>
        OperatingHours.Create(new TimeOnly(opens, 0), new TimeOnly(closes, 0)).Value;
}
