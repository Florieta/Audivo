namespace Audivo.Application.DTOs.Player;

/// <summary>
/// Full audiobook details returned when opening the player,
/// including the user's listening progress and bookmarks.
/// </summary>
public sealed record PlayerStateResponse(
    Guid AudiobookId,
    string Title,
    string Author,
    string? Genre,
    string? CoverImageUrl,
    string? AudioFileUrl,
    double TotalDurationSeconds,
    double LastPositionSeconds,
    DateTime? LastListenedAt,
    IReadOnlyList<BookmarkResponse> Bookmarks);
