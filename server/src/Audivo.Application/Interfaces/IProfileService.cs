using Audivo.Application.DTOs.Profile;

namespace Audivo.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse> GetCurrentProfileAsync(string userId, CancellationToken cancellationToken = default);

    Task<ProfileResponse> UpdateCurrentProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
