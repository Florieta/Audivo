using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Audivo.Infrastructure.Services;

/// <summary>
/// Stores uploaded files on local disk under wwwroot/uploads/{subfolder}/.
/// Returns a server-relative URL path so it can be swapped for blob storage later.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    // Most common audiobook and cover image formats accepted by the platform.
    private static readonly HashSet<string> AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];
    private static readonly HashSet<string> AllowedAudioExtensions = [".mp3", ".mp4", ".m4a", ".aac", ".wav", ".ogg", ".flac", ".webm"];

    private const long MaxImageSizeBytes = 5 * 1024 * 1024;   // 5 MB
    private const long MaxAudioSizeBytes = 500 * 1024 * 1024; // 500 MB

    private readonly string _webRootPath;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _webRootPath = environment.WebRootPath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public async Task<string> SaveFileAsync(FileUpload file, string subfolder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        ValidateFile(file, subfolder, extension);

        var uploadsDir = Path.Combine(_webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(uploadsDir);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var physicalPath = Path.Combine(uploadsDir, uniqueFileName);

        await using var sourceStream = file.Content;
        await using var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await sourceStream.CopyToAsync(fileStream, cancellationToken);

        // Return a server-relative URL (e.g. /uploads/covers/abc.jpg)
        return $"/uploads/{subfolder}/{uniqueFileName}";
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        // Convert /uploads/covers/abc.jpg → wwwroot/uploads/covers/abc.jpg
        var normalised = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_webRootPath, normalised);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }

    private static void ValidateFile(FileUpload file, string subfolder, string extension)
    {
        bool isImage = subfolder.Equals("covers", StringComparison.OrdinalIgnoreCase)
            || subfolder.Equals("profiles", StringComparison.OrdinalIgnoreCase);
        var allowedExtensions = isImage ? AllowedImageExtensions : AllowedAudioExtensions;
        var maxSize = isImage ? MaxImageSizeBytes : MaxAudioSizeBytes;

        if (!allowedExtensions.Contains(extension))
        {
            var allowed = string.Join(", ", allowedExtensions);
            throw new ValidationException(
                isImage ? "coverImage" : "audioFile",
                $"Unsupported file type '{extension}'. Allowed: {allowed}.");
        }

        if (file.Size > maxSize)
        {
            var limitMb = maxSize / (1024 * 1024);
            throw new ValidationException(
                isImage ? "coverImage" : "audioFile",
                $"File exceeds the maximum allowed size of {limitMb} MB.");
        }
    }
}
