namespace TitanFitness.Api.Auth;

/// <summary>Bound from the "Auth" section of appsettings.json.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Secret used to sign tokens. Change it outside development.</summary>
    public string SigningKey { get; init; } = null!;

    public int TokenLifetimeMinutes { get; init; } = 480;

    public List<StaffAccount> Users { get; init; } = [];
}

public sealed class StaffAccount
{
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string Role { get; init; } = null!;

    /// <summary>Only for Member accounts: the membership number this account books for.</summary>
    public string? MembershipNumber { get; init; }
}
