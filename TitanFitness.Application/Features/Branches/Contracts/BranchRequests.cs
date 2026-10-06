using FluentValidation;
using TitanFitness.Domain.Branches;

namespace TitanFitness.Application.Features.Branches.Contracts;

/// <summary>Body of POST /api/branches and PUT /api/branches/{id}.</summary>
public sealed record BranchRequest(string Name, string? Address, TimeOnly Opens, TimeOnly Closes);

public sealed class BranchRequestValidator : AbstractValidator<BranchRequest>
{
    public BranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Branch.NameMaxLength);
        RuleFor(x => x.Address).MaximumLength(Branch.AddressMaxLength);
        RuleFor(x => x.Closes).GreaterThan(x => x.Opens).WithMessage("Closing time must be after opening time.");
    }
}

/// <summary>Body of POST /api/branches/{id}/studios and PUT /api/branches/{id}/studios/{studioId}.</summary>
public sealed record StudioRequest(string Name, int Capacity);

public sealed class StudioRequestValidator : AbstractValidator<StudioRequest>
{
    public StudioRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Studio.NameMaxLength);
        RuleFor(x => x.Capacity).InclusiveBetween(1, Studio.MaxCapacity);
    }
}
