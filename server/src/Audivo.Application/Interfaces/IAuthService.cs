using Audivo.Application.DTOs.Auth;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Application-level service for user authentication workflows.
/// All methods that issue tokens return both the auth response and a refresh token string
/// (the API layer is responsible for setting it as an httpOnly cookie).
/// </summary>
public interface IAuthService
{
    Task<(AuthResponse Response, string RefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<(AuthResponse Response, string RefreshToken)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the provided refresh token, rotates it, and issues a new access token.
    /// Returns the new refresh token string (to be set as a cookie by the API layer).
    /// </summary>
    Task<(AuthResponse Response, string NewRefreshToken)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
