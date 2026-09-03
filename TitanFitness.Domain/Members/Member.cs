using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Members;

public sealed class Member
{
    public Guid Id { get; private set; }
    public MembershipNumber Number { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public DateOnly JoinedOn { get; private set; }
    public string? PhotoUrl { get; private set; }
    public Guid HomeBranchId { get; private set; }

    private Member() { }

    public Member(MembershipNumber number, string fullName, DateOnly joinedOn, Guid homeBranchId)
    {
        ArgumentNullException.ThrowIfNull(number);

        if (homeBranchId == Guid.Empty)
            throw new ArgumentException("Home branch is required.", nameof(homeBranchId));

        Id = Guid.CreateVersion7();
        Number = number;
        JoinedOn = joinedOn;
        HomeBranchId = homeBranchId;

        Rename(fullName);
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        var trimmed = fullName.Trim();

        if (trimmed.Length > 100)
            throw new ArgumentException("Full name cannot exceed 100 characters.", nameof(fullName));

        FullName = trimmed;
    }

    public void UpdateContactDetails(string? email, string? phone, string? address)
    {
        Email = Optional(email, 100, nameof(email));
        Phone = Optional(phone, 20, nameof(phone));
        Address = Optional(address, 200, nameof(address));
    }

    public void SetPhoto(string? photoUrl) => PhotoUrl = Optional(photoUrl, 500, nameof(photoUrl));

    public void TransferToBranch(Guid branchId)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("Home branch is required.", nameof(branchId));

        HomeBranchId = branchId;
    }

    private static string? Optional(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{field} cannot exceed {maxLength} characters.", field);

        return trimmed;
    }
}