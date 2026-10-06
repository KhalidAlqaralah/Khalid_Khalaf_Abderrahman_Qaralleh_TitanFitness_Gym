using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Trainers;

/// <summary>A trainer on the roster. Belongs to one home branch; only active trainers can be given classes.</summary>
public sealed class Trainer : AggregateRoot
{
    public const string CodePrefix = "TR-";
    public const int CodeMaxLength = 10;
    public const int NameMinLength = 2;
    public const int NameMaxLength = 80;
    public const int SpecialtyMaxLength = 100;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Specialty { get; private set; }
    public Guid BranchId { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Trainer()
    {
    }

    /// <summary>The trainer ID shown on screen: sequence 1042 becomes TR-1042.</summary>
    public static string CodeFor(int sequence) => $"{CodePrefix}{sequence:D4}";

    /// <summary>The number inside a trainer ID, or 0 when it does not follow the TR-NNNN pattern.</summary>
    public static int SequenceOf(string? code) =>
        code is not null && code.StartsWith(CodePrefix, StringComparison.Ordinal) && int.TryParse(code[CodePrefix.Length..], out var n) ? n : 0;

    public static Result<Trainer> Create(
        string code,
        string name,
        string? specialty,
        Guid branchId,
        string email,
        string? phone,
        bool isActive,
        string createdBy,
        DateTime createdAt)
    {
        var cleanCode = Guard.Required(code, CodeMaxLength, "code", "Trainer ID");
        if (cleanCode.IsFailure)
            return cleanCode.Error;

        var trainer = new Trainer
        {
            Id = Guid.CreateVersion7(),
            Code = cleanCode.Value,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
            CreatedAt = createdAt
        };

        var applied = trainer.Update(name, specialty, branchId, email, phone, isActive);
        if (applied.IsFailure)
            return applied.Error;

        return trainer;
    }

    public Result Update(string name, string? specialty, Guid branchId, string email, string? phone, bool isActive)
    {
        var cleanName = Guard.Required(name, NameMaxLength, "name", "Trainer name");
        if (cleanName.IsFailure)
            return cleanName.Error;

        if (cleanName.Value.Length < NameMinLength)
            return Error.Validation("Trainer.NameTooShort", $"Trainer name must be at least {NameMinLength} characters.", "name");

        var cleanSpecialty = Guard.Optional(specialty, SpecialtyMaxLength, "specialty", "Specialty");
        if (cleanSpecialty.IsFailure)
            return cleanSpecialty.Error;

        var branch = Guard.RequiredId(branchId, "branchId", "Branch");
        if (branch.IsFailure)
            return branch.Error;

        var cleanEmail = EmailAddress.Create(email);
        if (cleanEmail.IsFailure)
            return cleanEmail.Error;

        var cleanPhone = Guard.OptionalPhone(phone, "phone");
        if (cleanPhone.IsFailure)
            return cleanPhone.Error;

        Name = cleanName.Value;
        Specialty = cleanSpecialty.Value;
        BranchId = branch.Value;
        Email = cleanEmail.Value;
        Phone = cleanPhone.Value;
        IsActive = isActive;
        return Result.Success();
    }
}

public static class TrainerErrors
{
    public static readonly Error NotFound = Error.NotFound("Trainer.NotFound", "Trainer not found.");

    public static readonly Error NotActive =
        Error.Conflict("Trainer.NotActive", "Only an active trainer can be given a class.", "trainerId");

    public static readonly Error WrongBranch =
        Error.Validation("Trainer.WrongBranch", "The trainer does not work at the chosen branch.", "trainerId");

    public static readonly Error Busy =
        Error.Conflict("Trainer.Busy", "This trainer already teaches a class at an overlapping time.", "trainerId");

    public static Error DuplicateEmail(string email) =>
        Error.Conflict("Trainer.DuplicateEmail", $"A trainer with this email already exists ({email}).", "email");
}
