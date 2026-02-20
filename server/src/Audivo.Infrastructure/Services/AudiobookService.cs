using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Audivo.Core.Enums;
using Audivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Audivo.Infrastructure.Services;

/// <summary>
/// Manages the full lifecycle of audiobooks: creation, updates, deletion, and owner-scoped retrieval.
/// File uploads are coordinated with <see cref="IFileStorageService"/> with cleanup on failure.
/// </summary>
public sealed class AudiobookService : IAudiobookService
{
    private readonly AudivoDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public AudiobookService(AudivoDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<AudiobookResponse>> GetUserAudiobooksAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Audiobooks
            .Where(a => a.UploadedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AudiobookResponse(
                a.Id,
                a.Title,
                a.Author,
                a.Genre,
                a.CoverImageUrl,
                a.Chapters.OrderBy(c => c.OrderIndex).Select(c => c.AudioFileUrl).FirstOrDefault(),
                a.TotalDuration.TotalSeconds,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AudiobookResponse> CreateAudiobookAsync(
        CreateAudiobookRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var coverUrl = request.CoverImage is not null
            ? await _fileStorage.SaveFileAsync(request.CoverImage, "covers", cancellationToken)
            : null;

        string audioUrl;
        try
        {
            audioUrl = await _fileStorage.SaveFileAsync(request.AudioFile, "audio", cancellationToken);
        }
        catch
        {
            // If audio upload fails after cover was saved, clean up the cover file
            await _fileStorage.DeleteFileAsync(coverUrl, cancellationToken);
            throw;
        }

        var audiobook = new Audiobook
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Genre = request.Genre?.Trim(),
            CoverImageUrl = coverUrl,
            UploadedByUserId = userId,
        };

        var chapter = new Chapter
        {
            Title = "Chapter 1",
            OrderIndex = 1,
            AudioFileUrl = audioUrl,
            AudiobookId = audiobook.Id,
        };

        audiobook.Chapters.Add(chapter);

        _db.Audiobooks.Add(audiobook);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToResponse(audiobook);
    }

    public async Task<AudiobookResponse> UpdateAudiobookAsync(
        Guid id, UpdateAudiobookRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);

        var audiobook = await _db.Audiobooks
            .Include(a => a.Chapters)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Audiobook), id);

        if (audiobook.UploadedByUserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        if (request.Title is not null)
        {
            audiobook.Title = request.Title.Trim();
        }

        if (request.Author is not null)
        {
            audiobook.Author = request.Author.Trim();
        }

        if (request.Genre is not null)
        {
            audiobook.Genre = request.Genre.Trim();
        }

        if (request.CoverImage is not null)
        {
            var oldCover = audiobook.CoverImageUrl;
            audiobook.CoverImageUrl = await _fileStorage.SaveFileAsync(request.CoverImage, "covers", cancellationToken);
            await _fileStorage.DeleteFileAsync(oldCover, cancellationToken);
        }

        if (request.AudioFile is not null)
        {
            var chapter = audiobook.Chapters.OrderBy(c => c.OrderIndex).FirstOrDefault();
            var newAudio = await _fileStorage.SaveFileAsync(request.AudioFile, "audio", cancellationToken);

            if (chapter is null)
            {
                audiobook.Chapters.Add(new Chapter
                {
                    Title = "Chapter 1",
                    OrderIndex = 1,
                    AudioFileUrl = newAudio,
                    AudiobookId = audiobook.Id,
                });
            }
            else
            {
                var oldAudio = chapter.AudioFileUrl;
                chapter.AudioFileUrl = newAudio;
                await _fileStorage.DeleteFileAsync(oldAudio, cancellationToken);
            }
        }

        audiobook.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToResponse(audiobook);
    }

    public async Task DeleteAudiobookAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var audiobook = await _db.Audiobooks
            .Include(a => a.Chapters)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Audiobook), id);

        if (audiobook.UploadedByUserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        // Clean up files before removing the DB record
        await _fileStorage.DeleteFileAsync(audiobook.CoverImageUrl, cancellationToken);
        foreach (var chapter in audiobook.Chapters)
        {
            await _fileStorage.DeleteFileAsync(chapter.AudioFileUrl, cancellationToken);
        }

        _db.Audiobooks.Remove(audiobook);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCreateRequest(CreateAudiobookRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Author))
        {
            errors["author"] = ["Author is required."];
        }

        if (!string.IsNullOrWhiteSpace(request.Genre) && !IsValidGenre(request.Genre))
        {
            errors["genre"] = [$"Genre must be one of: {GetAllowedGenresDescription()}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateUpdateRequest(UpdateAudiobookRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title cannot be empty."];
        }

        if (request.Author is not null && string.IsNullOrWhiteSpace(request.Author))
        {
            errors["author"] = ["Author cannot be empty."];
        }

        if (request.Genre is not null && !string.IsNullOrWhiteSpace(request.Genre) && !IsValidGenre(request.Genre))
        {
            errors["genre"] = [$"Genre must be one of: {GetAllowedGenresDescription()}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static bool IsValidGenre(string genre) =>
        Enum.TryParse<AudiobookGenre>(genre.Trim(), ignoreCase: true, out _);

    private static string GetAllowedGenresDescription() =>
        string.Join(", ", Enum.GetNames<AudiobookGenre>());

    private static AudiobookResponse MapToResponse(Audiobook audiobook)
    {
        var firstChapter = audiobook.Chapters.OrderBy(c => c.OrderIndex).FirstOrDefault();
        return new AudiobookResponse(
            audiobook.Id,
            audiobook.Title,
            audiobook.Author,
            audiobook.Genre,
            audiobook.CoverImageUrl,
            firstChapter?.AudioFileUrl,
            audiobook.TotalDuration.TotalSeconds,
            audiobook.CreatedAt);
    }
}
