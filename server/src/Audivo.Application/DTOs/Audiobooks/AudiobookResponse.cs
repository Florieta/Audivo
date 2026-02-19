namespace Audivo.Application.DTOs.Audiobooks;

public sealed record AudiobookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Genre,
    string? CoverImageUrl,
    string? AudioFileUrl,
    double TotalDurationSeconds,
    DateTime CreatedAt);
