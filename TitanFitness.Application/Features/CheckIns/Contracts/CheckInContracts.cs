using FluentValidation;
using TitanFitness.Domain.CheckIns;

namespace TitanFitness.Application.Features.CheckIns.Contracts;

/// <summary>Body of POST /api/check-ins (the New Check-in dialog).</summary>
public sealed record RecordCheckInRequest(Guid? MemberId, Guid? BranchId, DateOnly? Date, TimeOnly? Time, string? Notes);

public sealed class RecordCheckInRequestValidator : AbstractValidator<RecordCheckInRequest>
{
    public RecordCheckInRequestValidator()
    {
        RuleFor(x => x.MemberId).Must(id => id is not null && id != Guid.Empty).WithMessage("Member is required.");
        RuleFor(x => x.BranchId).Must(id => id is not null && id != Guid.Empty).WithMessage("Branch is required.");
        RuleFor(x => x.Date).NotNull().WithMessage("Check-in date is required.");
        RuleFor(x => x.Time).NotNull().WithMessage("Check-in time is required.");
        RuleFor(x => x.Notes).MaximumLength(CheckIn.NotesMaxLength);
    }
}

public sealed record CheckInResponse(Guid Id, Guid MemberId, string MemberName, Guid BranchId, string BranchName, DateTime OccurredAt);
