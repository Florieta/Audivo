using Audivo.Application.DTOs.Audiobooks;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Manages the full lifecycle of audiobooks uploaded by users,
/// including creation, updates, deletion, and ownership-based retrieval.
/// </summary>
public interface IAudiobookService
{
    Task<IReadOnlyList<AudiobookResponse>> GetUserAudiobooksAsync(string userId, CancellationToken cancellationToken = default);

    Task<AudiobookResponse> CreateAudiobookAsync(CreateAudiobookRequest request, string userId, CancellationToken cancellationToken = default);

    Task<AudiobookResponse> UpdateAudiobookAsync(Guid id, UpdateAudiobookRequest request, string userId, CancellationToken cancellationToken = default);

    Task DeleteAudiobookAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
