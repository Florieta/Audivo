using Audivo.Application.DTOs.Library;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Audivo.Infrastructure.Services;

public sealed class LibraryService : ILibraryService
{
    private readonly AudivoDbContext _db;

    private sealed record ProgressInfo(double ListenedSeconds, DateTime? LastListenedAt);

    public LibraryService(AudivoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LibraryAudiobookResponse>> GetGalleryAudiobooksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favoriteIds = await GetFavoriteIdsAsync(userId, cancellationToken);

        var books = await _db.Audiobooks
            .OrderBy(a => a.Genre)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Author,
                a.Genre,
                a.CoverImageUrl,
                AudioFileUrl = a.Chapters.OrderBy(c => c.OrderIndex).Select(c => c.AudioFileUrl).FirstOrDefault(),
                TotalDurationSeconds = a.TotalDuration.TotalSeconds,
                a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var progressLookup = await GetProgressLookupAsync(userId, books.Select(b => b.Id).ToList(), cancellationToken);

        return books.Select(a => new LibraryAudiobookResponse(
            a.Id,
            a.Title,
            a.Author,
            a.Genre,
            a.CoverImageUrl,
            a.AudioFileUrl,
            a.TotalDurationSeconds,
            GetListenedSeconds(progressLookup, a.Id),
            GetProgressPercent(progressLookup, a.Id, a.TotalDurationSeconds),
            IsCompleted(progressLookup, a.Id, a.TotalDurationSeconds),
            GetLastListenedAt(progressLookup, a.Id),
            a.CreatedAt,
            favoriteIds.Contains(a.Id))).ToList();
    }

    public async Task<IReadOnlyList<LibraryAudiobookResponse>> GetUploadedBooksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favoriteIds = await GetFavoriteIdsAsync(userId, cancellationToken);

        var books = await _db.Audiobooks
            .Where(a => a.UploadedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Author,
                a.Genre,
                a.CoverImageUrl,
                AudioFileUrl = a.Chapters.OrderBy(c => c.OrderIndex).Select(c => c.AudioFileUrl).FirstOrDefault(),
                TotalDurationSeconds = a.TotalDuration.TotalSeconds,
                a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var progressLookup = await GetProgressLookupAsync(userId, books.Select(b => b.Id).ToList(), cancellationToken);

        return books.Select(a => new LibraryAudiobookResponse(
            a.Id,
            a.Title,
            a.Author,
            a.Genre,
            a.CoverImageUrl,
            a.AudioFileUrl,
            a.TotalDurationSeconds,
            GetListenedSeconds(progressLookup, a.Id),
            GetProgressPercent(progressLookup, a.Id, a.TotalDurationSeconds),
            IsCompleted(progressLookup, a.Id, a.TotalDurationSeconds),
            GetLastListenedAt(progressLookup, a.Id),
            a.CreatedAt,
            favoriteIds.Contains(a.Id))).ToList();
    }

    public async Task<IReadOnlyList<LibraryAudiobookResponse>> GetFavoriteBooksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var books = await _db.FavoriteAudiobooks
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .Select(f => new
            {
                Id = f.Audiobook.Id,
                f.Audiobook.Title,
                f.Audiobook.Author,
                f.Audiobook.Genre,
                f.Audiobook.CoverImageUrl,
                AudioFileUrl = f.Audiobook.Chapters.OrderBy(c => c.OrderIndex).Select(c => c.AudioFileUrl).FirstOrDefault(),
                TotalDurationSeconds = f.Audiobook.TotalDuration.TotalSeconds,
                CreatedAt = f.Audiobook.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var progressLookup = await GetProgressLookupAsync(userId, books.Select(b => b.Id).ToList(), cancellationToken);

        return books.Select(f => new LibraryAudiobookResponse(
            f.Id,
            f.Title,
            f.Author,
            f.Genre,
            f.CoverImageUrl,
            f.AudioFileUrl,
            f.TotalDurationSeconds,
            GetListenedSeconds(progressLookup, f.Id),
            GetProgressPercent(progressLookup, f.Id, f.TotalDurationSeconds),
            IsCompleted(progressLookup, f.Id, f.TotalDurationSeconds),
            GetLastListenedAt(progressLookup, f.Id),
            f.CreatedAt,
            true)).ToList();
    }

    public async Task<IReadOnlyList<LibraryAudiobookResponse>> SearchAudiobooksAsync(
        string userId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var trimmed = searchTerm.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        var pattern = $"%{trimmed}%";
        var favoriteIds = await GetFavoriteIdsAsync(userId, cancellationToken);

        var results = await _db.Audiobooks
            .Where(a => EF.Functions.Like(a.Title, pattern)
                        || EF.Functions.Like(a.Author, pattern)
                        || (a.Genre != null && EF.Functions.Like(a.Genre, pattern)))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Author,
                a.Genre,
                a.CoverImageUrl,
                AudioFileUrl = a.Chapters.OrderBy(c => c.OrderIndex).Select(c => c.AudioFileUrl).FirstOrDefault(),
                TotalDurationSeconds = a.TotalDuration.TotalSeconds,
                a.CreatedAt,
            })
            .Take(60)
            .ToListAsync(cancellationToken);

        var progressLookup = await GetProgressLookupAsync(userId, results.Select(r => r.Id).ToList(), cancellationToken);

        return results.Select(a => new LibraryAudiobookResponse(
            a.Id,
            a.Title,
            a.Author,
            a.Genre,
            a.CoverImageUrl,
            a.AudioFileUrl,
            a.TotalDurationSeconds,
            GetListenedSeconds(progressLookup, a.Id),
            GetProgressPercent(progressLookup, a.Id, a.TotalDurationSeconds),
            IsCompleted(progressLookup, a.Id, a.TotalDurationSeconds),
            GetLastListenedAt(progressLookup, a.Id),
            a.CreatedAt,
            favoriteIds.Contains(a.Id))).ToList();
    }

    public async Task AddToFavoritesAsync(Guid audiobookId, string userId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Audiobooks.AnyAsync(a => a.Id == audiobookId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Audiobook), audiobookId);
        }

        var alreadyFavorite = await _db.FavoriteAudiobooks
            .AnyAsync(f => f.UserId == userId && f.AudiobookId == audiobookId, cancellationToken);

        if (alreadyFavorite)
        {
            return;
        }

        _db.FavoriteAudiobooks.Add(new FavoriteAudiobook
        {
            UserId = userId,
            AudiobookId = audiobookId,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromFavoritesAsync(Guid audiobookId, string userId, CancellationToken cancellationToken = default)
    {
        var favorite = await _db.FavoriteAudiobooks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.AudiobookId == audiobookId, cancellationToken);

        if (favorite is null)
        {
            return;
        }

        _db.FavoriteAudiobooks.Remove(favorite);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<HashSet<Guid>> GetFavoriteIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var favoriteIds = await _db.FavoriteAudiobooks
            .Where(f => f.UserId == userId)
            .Select(f => f.AudiobookId)
            .ToListAsync(cancellationToken);

        return favoriteIds.ToHashSet();
    }

    private async Task<Dictionary<Guid, ProgressInfo>> GetProgressLookupAsync(
        string userId,
        List<Guid> audiobookIds,
        CancellationToken cancellationToken)
    {
        if (audiobookIds.Count == 0)
        {
            return [];
        }

        var progressRows = await _db.ListeningProgressRecords
            .Where(p => p.UserId == userId && audiobookIds.Contains(p.AudiobookId))
            .Select(p => new
            {
                p.AudiobookId,
                ListenedSeconds = p.PositionInChapter.TotalSeconds,
                p.LastListenedAt,
            })
            .ToListAsync(cancellationToken);

        return progressRows.ToDictionary(
            p => p.AudiobookId,
            p => new ProgressInfo(p.ListenedSeconds, p.LastListenedAt));
    }

    private static double GetListenedSeconds(Dictionary<Guid, ProgressInfo> progressLookup, Guid audiobookId)
        => progressLookup.TryGetValue(audiobookId, out var progress)
            ? Math.Max(0, progress.ListenedSeconds)
            : 0;

    private static DateTime? GetLastListenedAt(Dictionary<Guid, ProgressInfo> progressLookup, Guid audiobookId)
        => progressLookup.TryGetValue(audiobookId, out var progress)
            ? progress.LastListenedAt
            : null;

    private static double GetProgressPercent(
        Dictionary<Guid, ProgressInfo> progressLookup,
        Guid audiobookId,
        double totalDurationSeconds)
    {
        if (totalDurationSeconds <= 0)
        {
            return 0;
        }

        var listened = GetListenedSeconds(progressLookup, audiobookId);
        return Math.Clamp((listened / totalDurationSeconds) * 100, 0, 100);
    }

    private static bool IsCompleted(
        Dictionary<Guid, ProgressInfo> progressLookup,
        Guid audiobookId,
        double totalDurationSeconds)
    {
        if (totalDurationSeconds <= 0)
        {
            return false;
        }

        return GetListenedSeconds(progressLookup, audiobookId) >= totalDurationSeconds - 1;
    }
}
