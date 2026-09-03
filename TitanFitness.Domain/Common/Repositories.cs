using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Common;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct = default);
    void Add(Branch branch);
}

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Member?> GetByNumberAsync(MembershipNumber number, CancellationToken ct = default);
    Task<bool> NumberExistsAsync(MembershipNumber number, CancellationToken ct = default);
    void Add(Member member);
}

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Plan>> ListPublishedAsync(CancellationToken ct = default);
    void Add(Plan plan);
}

public interface IMembershipRepository
{
    Task<Membership?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Membership?> GetActiveForMemberAsync(Guid memberId, DateOnly on, CancellationToken ct = default);
    Task<bool> HasOverlappingMembershipAsync(Guid memberId, DateRange period, CancellationToken ct = default);
    void Add(Membership membership);
}

public interface ITrainerRepository
{
    Task<Trainer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Trainer>> ListActiveAsync(CancellationToken ct = default);
    void Add(Trainer trainer);
}

public interface IClassSessionRepository
{
    Task<ClassSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> TrainerIsBusyAsync(Guid trainerId, TimeSlot slot, Guid? excludingSessionId = null, CancellationToken ct = default);
    Task<bool> StudioIsBusyAsync(Guid studioId, TimeSlot slot, Guid? excludingSessionId = null, CancellationToken ct = default);
    Task<bool> MemberHasOverlappingBookingAsync(Guid memberId, TimeSlot slot, CancellationToken ct = default);
    void Add(ClassSession session);
}

public interface ICheckInRepository
{
    Task<int> CountAdmittedOnAsync(Guid? branchId, DateOnly date, CancellationToken ct = default);
    void Add(CheckIn checkIn);
}