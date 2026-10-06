using FluentValidation;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Contracts;

/// <summary>Body of POST /api/trainers and PUT /api/trainers/{id}.</summary>
public sealed record TrainerRequest(string Name, string? Specialty, Guid? BranchId, string Email, string? Phone, bool IsActive = true);

public sealed class TrainerRequestValidator : AbstractValidator<TrainerRequest>
{
    public TrainerRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Trainer name is required.")
            .Must(n => n is null || n.Trim().Length is >= Trainer.NameMinLength and <= Trainer.NameMaxLength)
            .WithMessage($"Trainer name must be {Trainer.NameMinLength}–{Trainer.NameMaxLength} characters.");

        RuleFor(x => x.Specialty).MaximumLength(Trainer.SpecialtyMaxLength);

        RuleFor(x => x.BranchId)
            .Must(id => id is not null && id != Guid.Empty).WithMessage("Branch is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-()]{6,20}$").When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Enter a valid phone number, e.g. +1 (555) 000-0000.");
    }
}

/// <summary>[FromQuery] object for GET /api/trainers.</summary>
public sealed class GetTrainersRequest : PagedRequest
{
    public List<Guid>? BranchIds { get; init; }

    public List<string>? Specialties { get; init; }

    /// <summary>"Active", "Inactive" or both. Empty means all.</summary>
    public List<string>? Statuses { get; init; }
}

public sealed class GetTrainersRequestValidator : AbstractValidator<GetTrainersRequest>
{
    private static readonly string[] SortColumns = ["name", "code", "specialty", "branch", "status"];

    public GetTrainersRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagedRequest.MaxPageSize);
        RuleFor(x => x.SortBy)
            .Must(s => s is null || SortColumns.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortColumns)}.");
        RuleForEach(x => x.Statuses)
            .Must(s => s is "Active" or "Inactive").WithMessage("Status must be Active or Inactive.");
    }
}

/// <summary>[FromQuery] object for GET /api/trainers/lookup (the class dialog's instructor dropdown).</summary>
public sealed class GetTrainerLookupRequest
{
    public Guid? BranchId { get; init; }

    public bool ActiveOnly { get; init; } = true;
}
