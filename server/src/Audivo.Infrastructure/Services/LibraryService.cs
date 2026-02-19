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

        return books.Select(a => new LibraryAudiobookResponse(
            a.Id,
            a.Title,
            a.Author,
            a.Genre,
            a.CoverImageUrl,
            a.AudioFileUrl,
            a.TotalDurationSeconds,
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

        return books.Select(a => new LibraryAudiobookResponse(
            a.Id,
            a.Title,
            a.Author,
            a.Genre,
            a.CoverImageUrl,
            a.AudioFileUrl,
            a.TotalDurationSeconds,
            a.CreatedAt,
            favoriteIds.Contains(a.Id))).ToList();
    }

    public async Task<IReadOnlyList<LibraryAudiobookResponse>> GetFavoriteBooksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.FavoriteAudiobooks
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .Select(f => new LibraryAudiobookResponse(
                f.Audiobook.Id,
                f.Audiobook.Title,
                f.Audiobook.Author,
                f.Audiobook.Genre,
                f.Audiobook.CoverImageUrl,
                f.Audiobook.Chapters.OrderBy(c => c.OrderIndex).Select(c => c.AudioFileUrl).FirstOrDefault(),
                f.Audiobook.TotalDuration.TotalSeconds,
                f.Audiobook.CreatedAt,
                true))
            .ToListAsync(cancellationToken);
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

        return results.Select(a => new LibraryAudiobookResponse(
            a.Id,
            a.Title,
            a.Author,
            a.Genre,
            a.CoverImageUrl,
            a.AudioFileUrl,
            a.TotalDurationSeconds,
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
}
