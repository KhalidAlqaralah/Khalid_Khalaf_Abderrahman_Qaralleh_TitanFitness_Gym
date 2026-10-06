using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TitanFitness.Api.Auth;

/// <summary>Reads "Authorization: Bearer &lt;token&gt;" and turns a valid token into the signed-in user.</summary>
public sealed class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TokenService tokens) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";
    public const string MemberIdClaim = "member_id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var payload = tokens.Validate(header["Bearer ".Length..].Trim());
        if (payload is null)
            return Task.FromResult(AuthenticateResult.Fail("The session has expired or the token is not valid."));

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, payload.UserName),
            new(ClaimTypes.GivenName, payload.DisplayName),
            new(ClaimTypes.Role, payload.Role)
        };

        if (payload.MemberId is not null)
            claims.Add(new Claim(MemberIdClaim, payload.MemberId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
