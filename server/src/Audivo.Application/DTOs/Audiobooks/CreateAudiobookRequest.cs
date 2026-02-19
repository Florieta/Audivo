namespace Audivo.Application.DTOs.Audiobooks;

public sealed record CreateAudiobookRequest(
    string Title,
    string Author,
    string? Genre,
    FileUpload? CoverImage,
    FileUpload AudioFile);
