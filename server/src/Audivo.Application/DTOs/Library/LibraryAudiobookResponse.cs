namespace Audivo.Application.DTOs.Library;

/// <summary>
/// Audiobook entry enriched with the requesting user's listening progress and favourite status,
/// used throughout the library views (gallery, uploaded, favourites, search).
/// </summary>
public sealed record LibraryAudiobookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Genre,
    string? Description,
    string? CoverImageUrl,
    string? AudioFileUrl,
    double TotalDurationSeconds,
    double ListenedSeconds,
    double ProgressPercent,
    bool IsCompleted,
    DateTime? LastListenedAt,
    DateTime CreatedAt,
    bool IsFavorite);
