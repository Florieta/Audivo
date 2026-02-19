namespace Audivo.Application.DTOs.Auth;

/// <summary>Credentials supplied by the user when signing in.</summary>
public sealed record LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
