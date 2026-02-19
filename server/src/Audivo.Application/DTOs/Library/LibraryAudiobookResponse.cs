namespace Audivo.Application.DTOs.Library;

public sealed record LibraryAudiobookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Genre,
    string? CoverImageUrl,
    string? AudioFileUrl,
    double TotalDurationSeconds,
    double ListenedSeconds,
    double ProgressPercent,
    bool IsCompleted,
    DateTime? LastListenedAt,
    DateTime CreatedAt,
    bool IsFavorite);
