namespace Audivo.Application.DTOs.Audiobooks;

public sealed record UpdateAudiobookRequest(
    string? Title,
    string? Author,
    string? Genre,
    FileUpload? CoverImage,
    FileUpload? AudioFile);
