using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Security;

/// <summary>Creates sign-in tokens. Implemented in Infrastructure.</summary>
public interface ITokenService
{
    /// <summary>A short-lived JWT the portal sends as "Authorization: Bearer ...".</summary>
    AccessToken CreateAccessToken(User user);

    /// <summary>A long random refresh token. Only its hash is stored.</summary>
    NewRefreshToken CreateRefreshToken();

    /// <summary>The hash stored for a refresh token.</summary>
    string HashRefreshToken(string refreshToken);
}
