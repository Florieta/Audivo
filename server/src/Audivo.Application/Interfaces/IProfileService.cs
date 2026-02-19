using Audivo.Application.DTOs.Profile;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Provides operations for reading and updating the authenticated user's profile,
/// including personal details and profile photo management.
/// </summary>
public interface IProfileService
{
    Task<ProfileResponse> GetCurrentProfileAsync(string userId, CancellationToken cancellationToken = default);

    Task<ProfileResponse> UpdateCurrentProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
