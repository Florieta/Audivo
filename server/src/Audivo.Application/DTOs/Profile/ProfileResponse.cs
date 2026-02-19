namespace Audivo.Application.DTOs.Profile;

public sealed record ProfileResponse(
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? ProfileImageUrl,
    DateTime CreatedAt);
