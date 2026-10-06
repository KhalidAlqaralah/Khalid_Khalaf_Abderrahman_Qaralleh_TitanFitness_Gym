using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace TitanFitness.Api.Auth;

public sealed record TokenPayload(string UserName, string DisplayName, string Role, Guid? MemberId, DateTime ExpiresAtUtc);

/// <summary>
/// A small signed bearer token: base64url(JSON payload) + "." + base64url(HMAC-SHA256).
/// Stateless, so it survives an API restart until it expires.
/// </summary>
public sealed class TokenService(IOptions<AuthOptions> options, TimeProvider timeProvider)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.SigningKey);

    public (string Token, DateTime ExpiresAtUtc) Issue(string userName, string displayName, string role, Guid? memberId)
    {
        var expires = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(options.Value.TokenLifetimeMinutes);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new TokenPayload(userName, displayName, role, memberId, expires));
        var body = WebEncoders.Base64UrlEncode(payload);
        return ($"{body}.{Sign(body)}", expires);
    }

    public TokenPayload? Validate(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        var expected = Encoding.ASCII.GetBytes(Sign(parts[0]));
        var actual = Encoding.ASCII.GetBytes(parts[1]);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<TokenPayload>(WebEncoders.Base64UrlDecode(parts[0]));
            return payload is null || payload.ExpiresAtUtc <= timeProvider.GetUtcNow().UtcDateTime ? null : payload;
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return null;
        }
    }

    private string Sign(string body) => WebEncoders.Base64UrlEncode(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(body)));
}
