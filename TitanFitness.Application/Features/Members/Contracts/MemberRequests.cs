using FluentValidation;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Members;

namespace TitanFitness.Application.Features.Members.Contracts;

/// <summary>Body of POST /api/members (the Add Member dialog). Contact details are optional extras.</summary>
public sealed record CreateMemberRequest(string FullName, Guid? HomeBranchId, string? Email = null, string? Phone = null, string? Address = null);

/// <summary>Body of PUT /api/members/{id} (the Edit Member dialog).</summary>
public sealed record UpdateMemberRequest(string FullName, Guid? HomeBranchId);

internal static class MemberRules
{
    public const string NamePattern = @"^\s*[\p{L}][\p{L}\s'\-]*$";

    public static void FullName<T>(IRuleBuilderInitial<T, string> rule) =>
        rule.Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Member name is required.")
            .Must(n => n is null || n.Trim().Length is >= Member.NameMinLength and <= Member.NameMaxLength)
            .WithMessage($"Member name must be {Member.NameMinLength}–{Member.NameMaxLength} characters.")
            .Matches(NamePattern)
            .WithMessage("Member name can only contain letters, spaces, hyphens and apostrophes.");
}

public sealed class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
    {
        MemberRules.FullName(RuleFor(x => x.FullName));
        RuleFor(x => x.HomeBranchId).Must(id => id is not null && id != Guid.Empty).WithMessage("Branch is required.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("Enter a valid email address.");
        RuleFor(x => x.Phone).Matches(@"^\+?[0-9\s\-()]{6,20}$").When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Enter a valid phone number.");
        RuleFor(x => x.Address).MaximumLength(Member.AddressMaxLength);
    }
}

public sealed class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        MemberRules.FullName(RuleFor(x => x.FullName));
        RuleFor(x => x.HomeBranchId).Must(id => id is not null && id != Guid.Empty).WithMessage("Branch is required.");
    }
}

/// <summary>[FromQuery] object for GET /api/members.</summary>
public sealed class GetMembersRequest : PagedRequest
{
    public Guid? BranchId { get; init; }

    public List<MemberStatus>? Statuses { get; init; }
}

public sealed class GetMembersRequestValidator : AbstractValidator<GetMembersRequest>
{
    private static readonly string[] SortColumns = ["name", "number", "status", "branch", "lastVisit"];

    public GetMembersRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagedRequest.MaxPageSize);
        RuleFor(x => x.SortBy)
            .Must(s => s is null || SortColumns.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortColumns)}.");
        RuleForEach(x => x.Statuses).IsInEnum();
    }
}

/// <summary>[FromQuery] object for GET /api/members/{id}/activity.</summary>
public sealed class GetMemberActivityRequest
{
    public int Take { get; init; } = 7;
}

public sealed class GetMemberActivityRequestValidator : AbstractValidator<GetMemberActivityRequest>
{
    public GetMemberActivityRequestValidator() => RuleFor(x => x.Take).InclusiveBetween(1, 50);
}
