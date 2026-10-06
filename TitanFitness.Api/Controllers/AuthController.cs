using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TitanFitness.Api.Auth;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.SelfService.Queries.FindMemberByNumber;

namespace TitanFitness.Api.Controllers;

public sealed record LoginRequest(string UserName, string Password);

public sealed record SessionResponse(string Token, DateTime ExpiresAtUtc, string UserName, string DisplayName, string Role, Guid? MemberId);

public sealed record MeResponse(string UserName, string DisplayName, string Role, Guid? MemberId);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IOptions<AuthOptions> options, TokenService tokens, ISender sender) : ControllerBase
{
    /// <summary>Signs in a staff or member account from appsettings and returns a bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var account = options.Value.Users.FirstOrDefault(u =>
            string.Equals(u.UserName, request.UserName?.Trim(), StringComparison.OrdinalIgnoreCase)
            && u.Password == request.Password);

        if (account is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed", detail: "Wrong user name or password.");

        Guid? memberId = null;
        if (account.Role == Roles.Member)
        {
            memberId = await sender.Send(new FindMemberByNumberQuery(account.MembershipNumber ?? string.Empty), cancellationToken);
            if (memberId is null)
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed",
                    detail: "This member account is not linked to a member record.");
        }

        var (token, expires) = tokens.Issue(account.UserName, account.DisplayName, account.Role, memberId);
        return Ok(new SessionResponse(token, expires, account.UserName, account.DisplayName, account.Role, memberId));
    }

    /// <summary>Who the current token belongs to.</summary>
    [HttpGet("me")]
    [Authorize]
    public MeResponse Me() => new(
        User.Identity!.Name!,
        User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity.Name!,
        User.FindFirstValue(ClaimTypes.Role)!,
        Guid.TryParse(User.FindFirstValue(TokenAuthenticationHandler.MemberIdClaim), out var id) ? id : null);
}
