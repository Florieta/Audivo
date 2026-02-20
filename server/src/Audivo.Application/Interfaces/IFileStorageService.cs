using Audivo.Application.DTOs.Audiobooks;

namespace Audivo.Application.Interfaces;

/// <summary>
/// Abstracts file persistence so the storage backend (local disk, Cloudinary, blob storage, etc.) can be swapped
/// without touching application or domain logic.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves an uploaded file to a subfolder and returns the publicly-accessible URL.
    /// </summary>
    Task<string> SaveFileAsync(FileUpload file, string subfolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously saved file given its URL or relative path. Silently ignores missing files.
    /// </summary>
    Task DeleteFileAsync(string? fileUrl, CancellationToken cancellationToken = default);
}
