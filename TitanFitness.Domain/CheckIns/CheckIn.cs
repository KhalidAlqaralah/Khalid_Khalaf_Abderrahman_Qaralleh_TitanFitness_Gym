using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.CheckIns;

/// <summary>One recorded entry of a member into a branch.</summary>
public sealed class CheckIn : AggregateRoot
{
    public const int NotesMaxLength = 250;
    public const int MaxDaysBack = 7;

    public Guid MemberId { get; private set; }
    public Guid BranchId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? Notes { get; private set; }
    public string RecordedBy { get; private set; } = null!;
    public DateTime RecordedAt { get; private set; }

    private CheckIn()
    {
    }

    /// <summary>
    /// Records an entry. The time cannot be in the future and the date cannot be more than
    /// <see cref="MaxDaysBack"/> days ago. Whether the member may enter is decided by their membership first.
    /// </summary>
    public static Result<CheckIn> Record(Guid memberId, Guid branchId, DateTime occurredAt, string? notes, string recordedBy, DateTime now)
    {
        var member = Guard.RequiredId(memberId, "memberId", "Member");
        if (member.IsFailure)
            return member.Error;

        var branch = Guard.RequiredId(branchId, "branchId", "Branch");
        if (branch.IsFailure)
            return branch.Error;

        if (occurredAt > now)
            return Error.Validation("CheckIn.InFuture", "The check-in date and time cannot be in the future.", "time");

        if (DateOnly.FromDateTime(occurredAt) < DateOnly.FromDateTime(now).AddDays(-MaxDaysBack))
            return Error.Validation("CheckIn.TooOld", $"The check-in date cannot be more than {MaxDaysBack} days ago.", "date");

        var cleanNotes = Guard.Optional(notes, NotesMaxLength, "notes", "Notes");
        if (cleanNotes.IsFailure)
            return cleanNotes.Error;

        return new CheckIn
        {
            Id = Guid.CreateVersion7(),
            MemberId = memberId,
            BranchId = branchId,
            OccurredAt = occurredAt,
            Notes = cleanNotes.Value,
            RecordedBy = string.IsNullOrWhiteSpace(recordedBy) ? "system" : recordedBy.Trim(),
            RecordedAt = now
        };
    }
}
