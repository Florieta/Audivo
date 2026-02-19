namespace Audivo.Application.DTOs.Audiobooks;

/// <summary>
/// Read model returned when querying or creating/updating an audiobook.
/// </summary>
public sealed record AudiobookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Genre,
    string? CoverImageUrl,
    string? AudioFileUrl,
    double TotalDurationSeconds,
    DateTime CreatedAt);
