using Audivo.Application.Configuration;
using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Audivo.Infrastructure.Services;

/// <summary>
/// Stores uploaded files in Cloudinary cloud storage, organised in folders (profiles, covers, audio).
/// Returns the Cloudinary secure URL for database storage.
/// </summary>
public sealed partial class CloudinaryFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];
    private static readonly HashSet<string> AllowedAudioExtensions = [".mp3", ".mp4", ".m4a", ".aac", ".wav", ".ogg", ".flac", ".webm"];

    private const long MaxImageSizeBytes = 5 * 1024 * 1024;   // 5 MB
    private const long MaxAudioSizeBytes = 500 * 1024 * 1024;  // 500 MB

    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not extract Cloudinary public_id from URL: {Url}")]
    partial void LogPublicIdExtractionFailed(string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted Cloudinary resource {PublicId}")]
    partial void LogResourceDeleted(string publicId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cloudinary delete returned '{Result}' for {PublicId}")]
    partial void LogDeleteUnexpectedResult(string result, string publicId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cloudinary image upload failed: {Error}")]
    partial void LogImageUploadFailed(string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cloudinary audio upload failed: {Error}")]
    partial void LogAudioUploadFailed(string error);

    public CloudinaryFileStorageService(
        IOptions<CloudinarySettings> options,
        ILogger<CloudinaryFileStorageService> logger)
    {
        _logger = logger;

        var settings = options.Value;
        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(FileUpload file, string subfolder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        ValidateFile(file, subfolder, extension);

        bool isImage = IsImageSubfolder(subfolder);
        var publicId = $"audivo/{subfolder}/{Guid.NewGuid()}";

        if (isImage)
        {
            return await UploadImageAsync(file, publicId, cancellationToken);
        }

        return await UploadAudioAsync(file, publicId, extension, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string? fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        var publicId = ExtractPublicId(fileUrl);
        if (string.IsNullOrEmpty(publicId))
        {
            LogPublicIdExtractionFailed(fileUrl);
            return;
        }

        var resourceType = IsAudioUrl(fileUrl) ? ResourceType.Video : ResourceType.Image;

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = resourceType,
        });

        if (result.Result == "ok")
        {
            LogResourceDeleted(publicId);
        }
        else
        {
            LogDeleteUnexpectedResult(result.Result, publicId);
        }
    }

    private async Task<string> UploadImageAsync(FileUpload file, string publicId, CancellationToken cancellationToken)
    {
        await using var stream = file.Content;

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            PublicId = publicId,
            Overwrite = true,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            LogImageUploadFailed(result.Error.Message);
            throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");
        }

        return result.SecureUrl.ToString();
    }

    private async Task<string> UploadAudioAsync(FileUpload file, string publicId, string extension, CancellationToken cancellationToken)
    {
        await using var stream = file.Content;

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            PublicId = $"{publicId}{extension}",
            Overwrite = true,
        };

        var result = await _cloudinary.UploadLargeRawAsync(uploadParams, cancellationToken: cancellationToken);

        if (result.Error is not null)
        {
            LogAudioUploadFailed(result.Error.Message);
            throw new InvalidOperationException($"Audio upload failed: {result.Error.Message}");
        }

        return result.SecureUrl.ToString();
    }

    /// <summary>
    /// Extracts the Cloudinary public ID from a secure URL.
    /// E.g. https://res.cloudinary.com/xxx/image/upload/v123/audivo/covers/abc.jpg → audivo/covers/abc
    /// </summary>
    private static string? ExtractPublicId(string url)
    {
        // Cloudinary URLs follow the pattern: .../upload/v{version}/{public_id}.{ext}
        const string uploadSegment = "/upload/";
        var uploadIndex = url.IndexOf(uploadSegment, StringComparison.OrdinalIgnoreCase);
        if (uploadIndex < 0)
        {
            return null;
        }

        var afterUpload = url[(uploadIndex + uploadSegment.Length)..];

        // Skip version segment (v123456789/)
        if (afterUpload.StartsWith('v') && afterUpload.Contains('/'))
        {
            afterUpload = afterUpload[(afterUpload.IndexOf('/') + 1)..];
        }

        // Remove file extension for image public IDs
        if (!IsAudioUrl(url))
        {
            var dotIndex = afterUpload.LastIndexOf('.');
            if (dotIndex > 0)
            {
                afterUpload = afterUpload[..dotIndex];
            }
        }

        return afterUpload;
    }

    private static bool IsAudioUrl(string url)
    {
        // Cloudinary audio/raw URLs contain /raw/upload/ in the path
        return url.Contains("/raw/upload/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageSubfolder(string subfolder) =>
        subfolder.Equals("covers", StringComparison.OrdinalIgnoreCase)
        || subfolder.Equals("profiles", StringComparison.OrdinalIgnoreCase);

    private static void ValidateFile(FileUpload file, string subfolder, string extension)
    {
        bool isImage = IsImageSubfolder(subfolder);
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
