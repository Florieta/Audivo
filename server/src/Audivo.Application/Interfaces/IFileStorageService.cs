using Audivo.Application.DTOs.Audiobooks;

namespace Audivo.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves an uploaded file to a subfolder and returns the server-relative URL path (e.g. /uploads/covers/abc.jpg).
    /// </summary>
    Task<string> SaveFileAsync(FileUpload file, string subfolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously saved file given its server-relative URL path. Silently ignores missing files.
    /// </summary>
    void DeleteFile(string? relativePath);
}
