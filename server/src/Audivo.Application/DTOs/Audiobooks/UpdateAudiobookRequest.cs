namespace Audivo.Application.DTOs.Audiobooks;

/// <summary>
/// Payload for a partial update of an audiobook. Only non-null fields are applied.
/// </summary>
public sealed record UpdateAudiobookRequest(
    string? Title,
    string? Author,
    string? Genre,
    FileUpload? CoverImage,
    FileUpload? AudioFile);
