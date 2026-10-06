using System.Security.Claims;
using TitanFitness.Application.Common;

namespace TitanFitness.Api.Auth;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public string UserName => User?.Identity?.Name ?? "system";

    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public Guid? MemberId =>
        Guid.TryParse(User?.FindFirstValue(TokenAuthenticationHandler.MemberIdClaim), out var id) ? id : null;
}

public static class Policies
{
    public const string Staff = "Staff";
    public const string Manager = "Manager";
    public const string Member = "Member";
}
