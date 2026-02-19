namespace Audivo.Application.DTOs.Auth;

/// <summary>Data required to create a new user account.</summary>
public sealed record RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }
}
