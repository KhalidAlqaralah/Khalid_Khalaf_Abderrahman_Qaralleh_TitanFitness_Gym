namespace TitanFitness.Application.Common;

/// <summary>The current local time of the gym. Inject this instead of calling DateTime.Now so handlers stay testable.</summary>
public interface IClock
{
    DateTime Now { get; }

    DateOnly Today => DateOnly.FromDateTime(Now);
}

/// <summary>The signed-in user making the request.</summary>
public interface ICurrentUser
{
    string UserName { get; }

    string Role { get; }

    /// <summary>Set only for a member using self-service booking.</summary>
    Guid? MemberId { get; }
}

public static class Roles
{
    public const string FrontDesk = "FrontDesk";
    public const string Manager = "Manager";
    public const string Member = "Member";
}
