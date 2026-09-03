using TitanFitness.Domain.Common;

namespace TitanFitness.Domain.CheckIns;

public sealed class CheckIn
{
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid BranchId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public CheckInResult Result { get; private set; }
    public string? RefusalReason { get; private set; }

    private CheckIn() { }

    private CheckIn(Guid memberId, Guid branchId, DateTime occurredAt, CheckInResult result, string? refusalReason)
    {
        if (memberId == Guid.Empty)
            throw new ArgumentException("Member is required.", nameof(memberId));

        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch is required.", nameof(branchId));

        Id = Guid.CreateVersion7();
        MemberId = memberId;
        BranchId = branchId;
        OccurredAt = occurredAt;
        Result = result;
        RefusalReason = refusalReason;
    }

    public static CheckIn Admit(Guid memberId, Guid branchId, DateTime nowUtc) =>
        new(memberId, branchId, nowUtc, CheckInResult.Admitted, null);

    public static CheckIn Refuse(Guid memberId, Guid branchId, DateTime nowUtc, string reason) =>
        new(memberId, branchId, nowUtc, CheckInResult.Refused, Text.Required(reason, 100, nameof(reason)));
}