namespace Audivo.Application.DTOs.Audiobooks;

/// <summary>
/// Represents an uploaded file, decoupled from any HTTP framework primitives.
/// Controllers map IFormFile → FileUpload before passing to the service layer.
/// </summary>
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Size);
