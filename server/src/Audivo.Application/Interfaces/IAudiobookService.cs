using Audivo.Application.DTOs.Audiobooks;

namespace Audivo.Application.Interfaces;

public interface IAudiobookService
{
    Task<IReadOnlyList<AudiobookResponse>> GetUserAudiobooksAsync(string userId, CancellationToken cancellationToken = default);

    Task<AudiobookResponse> CreateAudiobookAsync(CreateAudiobookRequest request, string userId, CancellationToken cancellationToken = default);

    Task<AudiobookResponse> UpdateAudiobookAsync(Guid id, UpdateAudiobookRequest request, string userId, CancellationToken cancellationToken = default);

    Task DeleteAudiobookAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
