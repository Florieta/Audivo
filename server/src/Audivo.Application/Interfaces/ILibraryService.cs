using Audivo.Application.DTOs.Library;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Provides read and management operations for the audiobook library,
/// including the public gallery, uploaded books, favourites, and full-text search.
/// </summary>
public interface ILibraryService
{
    Task<IReadOnlyList<LibraryAudiobookResponse>> GetGalleryAudiobooksAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns gallery audiobooks filtered by optional genre and/or author, with results
    /// sorted according to the specified order.
    /// </summary>
    Task<IReadOnlyList<LibraryAudiobookResponse>> GetGalleryFilteredAsync(
        string userId,
        string? genre = null,
        string? author = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a distinct, alphabetically sorted list of all authors that have at least one audiobook.
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctAuthorsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LibraryAudiobookResponse>> GetUploadedBooksAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LibraryAudiobookResponse>> GetFavoriteBooksAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LibraryAudiobookResponse>> SearchAudiobooksAsync(string userId, string searchTerm, CancellationToken cancellationToken = default);

    Task AddToFavoritesAsync(Guid audiobookId, string userId, CancellationToken cancellationToken = default);

    Task RemoveFromFavoritesAsync(Guid audiobookId, string userId, CancellationToken cancellationToken = default);
}
