using Audivo.Application.DTOs.Player;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Manages listening state for the audio player, including progress persistence and user bookmarks.
/// </summary>
public interface IPlayerService
{
    Task<PlayerStateResponse> GetPlayerStateAsync(Guid audiobookId, string userId, CancellationToken cancellationToken = default);

    Task SaveProgressAsync(Guid audiobookId, string userId, SaveProgressRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookmarkResponse>> GetBookmarksAsync(Guid audiobookId, string userId, CancellationToken cancellationToken = default);

    Task<BookmarkResponse> CreateBookmarkAsync(Guid audiobookId, string userId, CreateBookmarkRequest request, CancellationToken cancellationToken = default);

    Task DeleteBookmarkAsync(Guid audiobookId, Guid bookmarkId, string userId, CancellationToken cancellationToken = default);
}
