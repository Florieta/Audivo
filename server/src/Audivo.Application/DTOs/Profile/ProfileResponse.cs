namespace Audivo.Application.DTOs.Profile;

/// <summary>Profile information returned for the authenticated user.</summary>
public sealed record ProfileResponse(
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? ProfileImageUrl,
    DateTime CreatedAt);
