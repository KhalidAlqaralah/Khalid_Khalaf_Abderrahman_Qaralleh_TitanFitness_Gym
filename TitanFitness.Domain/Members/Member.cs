using System.Text.RegularExpressions;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Members;

/// <summary>
/// A person who has joined the gym. Being a member grants nothing on its own:
/// access comes from holding a <see cref="Memberships.Membership"/>.
/// </summary>
public sealed partial class Member : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 80;
    public const int AddressMaxLength = 200;
    public const int PhotoUrlMaxLength = 500;

    public MembershipNumber Number { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public EmailAddress? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public DateOnly JoinedOn { get; private set; }
    public string? PhotoUrl { get; private set; }
    public Guid HomeBranchId { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Member()
    {
    }

    public static Result<Member> Create(
        MembershipNumber number,
        string fullName,
        Guid homeBranchId,
        DateOnly joinedOn,
        string createdBy,
        DateTime createdAt,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            JoinedOn = joinedOn,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
            CreatedAt = createdAt
        };

        var details = member.Update(fullName, homeBranchId);
        if (details.IsFailure)
            return details.Error;

        var contact = member.UpdateContactDetails(email, phone, address);
        if (contact.IsFailure)
            return contact.Error;

        return member;
    }

    /// <summary>Changes what the Edit Member dialog can change: the name and the home branch.</summary>
    public Result Update(string fullName, Guid homeBranchId)
    {
        var name = ValidateName(fullName);
        if (name.IsFailure)
            return name.Error;

        var branch = Guard.RequiredId(homeBranchId, "homeBranchId", "Home branch");
        if (branch.IsFailure)
            return branch.Error;

        FullName = name.Value;
        HomeBranchId = branch.Value;
        return Result.Success();
    }

    public Result UpdateContactDetails(string? email, string? phone, string? address)
    {
        var cleanEmail = EmailAddress.CreateOptional(email);
        if (cleanEmail.IsFailure)
            return cleanEmail.Error;

        var cleanPhone = Guard.OptionalPhone(phone, "phone");
        if (cleanPhone.IsFailure)
            return cleanPhone.Error;

        var cleanAddress = Guard.Optional(address, AddressMaxLength, "address", "Address");
        if (cleanAddress.IsFailure)
            return cleanAddress.Error;

        Email = cleanEmail.Value;
        Phone = cleanPhone.Value;
        Address = cleanAddress.Value;
        return Result.Success();
    }

    public Result SetPhoto(string? photoUrl)
    {
        var url = Guard.Optional(photoUrl, PhotoUrlMaxLength, "photoUrl", "Photo URL");
        if (url.IsFailure)
            return url.Error;

        PhotoUrl = url.Value;
        return Result.Success();
    }

    private static Result<string> ValidateName(string fullName)
    {
        var name = Guard.Required(fullName, NameMaxLength, "fullName", "Member name");
        if (name.IsFailure)
            return name;

        var collapsed = Whitespace().Replace(name.Value, " ");

        if (collapsed.Length < NameMinLength)
            return Error.Validation("Member.NameTooShort", $"Member name must be at least {NameMinLength} characters.", "fullName");

        if (!NamePattern().IsMatch(collapsed))
            return Error.Validation("Member.NameInvalid",
                "Member name can only contain letters, spaces, hyphens and apostrophes.", "fullName");

        return collapsed;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^[\p{L}][\p{L}\s'\-]*$")]
    private static partial Regex NamePattern();
}

public static class MemberErrors
{
    public static readonly Error NotFound = Error.NotFound("Member.NotFound", "The member was not found.");
}
