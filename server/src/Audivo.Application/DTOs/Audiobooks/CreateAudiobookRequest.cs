namespace Audivo.Application.DTOs.Audiobooks;

/// <summary>Payload for creating a new audiobook with metadata, an optional cover image, and a required audio file.</summary>
public sealed record CreateAudiobookRequest(
    string Title,
    string Author,
    string? Genre,
    FileUpload? CoverImage,
    FileUpload AudioFile);
