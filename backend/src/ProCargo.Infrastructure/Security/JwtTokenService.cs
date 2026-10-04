using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common;
using ProCargo.Domain.Entities;

namespace ProCargo.Infrastructure.Security;

/// <summary>Creates signed JWT access tokens and random refresh tokens.</summary>
internal sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider clock) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(User user)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        DateTime expiresAt = now.AddMinutes(_settings.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [AppClaimTypes.UserId] = user.UserId.ToString(CultureInfo.InvariantCulture),
                [AppClaimTypes.Name] = user.FullName,
                [AppClaimTypes.Role] = user.Role,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt, _settings.AccessTokenMinutes * 60);
    }

    public NewRefreshToken CreateRefreshToken()
    {
        string token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        DateTime expiresAt = clock.GetUtcNow().UtcDateTime.AddDays(_settings.RefreshTokenDays);

        return new NewRefreshToken(token, HashRefreshToken(token), expiresAt);
    }

    public string HashRefreshToken(string refreshToken) => Sha256.Hex(refreshToken);
}
