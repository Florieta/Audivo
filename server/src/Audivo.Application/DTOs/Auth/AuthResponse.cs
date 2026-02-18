namespace Audivo.Application.DTOs.Auth;

/// <summary>
/// Response returned on successful login/register.
/// Contains only the access token — the refresh token is set as an httpOnly cookie.
/// </summary>
public sealed record AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }
}
