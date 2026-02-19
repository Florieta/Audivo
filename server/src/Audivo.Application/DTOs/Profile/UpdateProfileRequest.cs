using Audivo.Application.DTOs.Audiobooks;

namespace Audivo.Application.DTOs.Profile;

/// <summary>
/// Payload for updating the authenticated user's profile details and optional profile photo.
/// </summary>
public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    FileUpload? ProfileImage);
