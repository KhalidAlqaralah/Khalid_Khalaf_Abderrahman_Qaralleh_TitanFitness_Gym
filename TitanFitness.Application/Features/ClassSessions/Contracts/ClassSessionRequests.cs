using FluentValidation;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Contracts;

/// <summary>Body of POST /api/class-sessions (Add New Class) and PUT /api/class-sessions/{id} (Edit Class).</summary>
public sealed record ClassSessionRequest(
    string ClassName,
    Guid? BranchId,
    Guid? TrainerId,
    Guid? StudioId,
    DateOnly? Date,
    TimeOnly? StartTime,
    int? DurationInMinutes,
    int? CapacityLimit,
    string? Description);

public sealed class ClassSessionRequestValidator : AbstractValidator<ClassSessionRequest>
{
    public ClassSessionRequestValidator()
    {
        RuleFor(x => x.ClassName)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Class name is required.")
            .Must(n => n is null || n.Trim().Length is >= ClassSession.NameMinLength and <= ClassSession.NameMaxLength)
            .WithMessage($"Class name must be {ClassSession.NameMinLength}–{ClassSession.NameMaxLength} characters.");

        RuleFor(x => x.BranchId).Must(id => id is not null && id != Guid.Empty).WithMessage("Branch is required.");
        RuleFor(x => x.Date).NotNull().WithMessage("Date is required.");
        RuleFor(x => x.StartTime).NotNull().WithMessage("Start time is required.");

        RuleFor(x => x.DurationInMinutes)
            .NotNull().WithMessage("Duration is required.")
            .Must(d => d is null || ClassSession.AllowedDurations.Contains(d.Value)).WithMessage("Duration must be 30, 45 or 60 minutes.");

        RuleFor(x => x.CapacityLimit)
            .InclusiveBetween(ClassSession.MinCapacity, ClassSession.MaxCapacity)
            .When(x => x.CapacityLimit is not null)
            .WithMessage($"Capacity must be a whole number between {ClassSession.MinCapacity} and {ClassSession.MaxCapacity}.");

        RuleFor(x => x.Description).MaximumLength(ClassSession.DescriptionMaxLength);
    }
}

/// <summary>Body of POST /api/class-sessions/{id}/bookings (Book Session).</summary>
public sealed record BookSessionRequest(Guid? MemberId, string? Note);

public sealed class BookSessionRequestValidator : AbstractValidator<BookSessionRequest>
{
    public BookSessionRequestValidator()
    {
        RuleFor(x => x.MemberId).Must(id => id is not null && id != Guid.Empty).WithMessage("Select a member.");
        RuleFor(x => x.Note).MaximumLength(Booking.NoteMaxLength);
    }
}

/// <summary>Body of POST /api/me/bookings (Book Session — Member View).</summary>
public sealed record BookMySessionRequest(Guid? SessionId, string? Note);

public sealed class BookMySessionRequestValidator : AbstractValidator<BookMySessionRequest>
{
    public BookMySessionRequestValidator()
    {
        RuleFor(x => x.SessionId).Must(id => id is not null && id != Guid.Empty).WithMessage("Class is required.");
        RuleFor(x => x.Note).MaximumLength(Booking.NoteMaxLength);
    }
}

/// <summary>Body of POST /api/class-sessions/{id}/bookings/{bookingId}/attendance.</summary>
public sealed record MarkAttendanceRequest(bool Attended);

/// <summary>[FromQuery] object for GET /api/class-sessions and GET /api/class-sessions/capacity-overview.</summary>
public sealed class GetClassScheduleRequest
{
    /// <summary>Empty means All Branches.</summary>
    public Guid? BranchId { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>1 for the Day view, 7 for the Week view.</summary>
    public int Days { get; init; } = 1;

    public string? Search { get; init; }
}

public sealed class GetClassScheduleRequestValidator : AbstractValidator<GetClassScheduleRequest>
{
    public GetClassScheduleRequestValidator() =>
        RuleFor(x => x.Days).Must(d => d is 1 or 7).WithMessage("Days must be 1 (day view) or 7 (week view).");
}
