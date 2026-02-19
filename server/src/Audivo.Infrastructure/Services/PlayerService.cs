using Audivo.Application.DTOs.Player;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Audivo.Infrastructure.Services;

public sealed class PlayerService : IPlayerService
{
    private readonly AudivoDbContext _db;

    public PlayerService(AudivoDbContext db)
    {
        _db = db;
    }

    public async Task<PlayerStateResponse> GetPlayerStateAsync(
        Guid audiobookId, string userId, CancellationToken cancellationToken = default)
    {
        var audiobook = await _db.Audiobooks
            .Include(a => a.Chapters)
            .FirstOrDefaultAsync(a => a.Id == audiobookId, cancellationToken)
            ?? throw new NotFoundException(nameof(Audiobook), audiobookId);

        var progress = await _db.ListeningProgressRecords
            .FirstOrDefaultAsync(
                p => p.AudiobookId == audiobookId && p.UserId == userId,
                cancellationToken);

        var bookmarks = await _db.Bookmarks
            .Where(b => b.AudiobookId == audiobookId && b.UserId == userId)
            .OrderBy(b => b.PositionSeconds)
            .Select(b => new BookmarkResponse(b.Id, b.PositionSeconds, b.Label, b.CreatedAt))
            .ToListAsync(cancellationToken);

        var audioFileUrl = audiobook.Chapters
            .OrderBy(c => c.OrderIndex)
            .Select(c => c.AudioFileUrl)
            .FirstOrDefault();

        var lastPositionSeconds = progress?.PositionInChapter.TotalSeconds ?? 0;

        return new PlayerStateResponse(
            audiobook.Id,
            audiobook.Title,
            audiobook.Author,
            audiobook.Genre,
            audiobook.CoverImageUrl,
            audioFileUrl,
            audiobook.TotalDuration.TotalSeconds,
            lastPositionSeconds,
            progress?.LastListenedAt,
            bookmarks);
    }

    public async Task SaveProgressAsync(
        Guid audiobookId, string userId, SaveProgressRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PositionSeconds < 0)
        {
            throw new ValidationException("positionSeconds", "Position must be non-negative.");
        }

        // Verify audiobook exists
        var audiobookExists = await _db.Audiobooks
            .AnyAsync(a => a.Id == audiobookId, cancellationToken);

        if (!audiobookExists)
        {
            throw new NotFoundException(nameof(Audiobook), audiobookId);
        }

        var progress = await _db.ListeningProgressRecords
            .FirstOrDefaultAsync(
                p => p.AudiobookId == audiobookId && p.UserId == userId,
                cancellationToken);

        if (progress is null)
        {
            progress = new ListeningProgress
            {
                UserId = userId,
                AudiobookId = audiobookId,
                PositionInChapter = TimeSpan.FromSeconds(request.PositionSeconds),
                LastListenedAt = DateTime.UtcNow,
            };
            _db.ListeningProgressRecords.Add(progress);
        }
        else
        {
            progress.PositionInChapter = TimeSpan.FromSeconds(request.PositionSeconds);
            progress.LastListenedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookmarkResponse>> GetBookmarksAsync(
        Guid audiobookId, string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookmarks
            .Where(b => b.AudiobookId == audiobookId && b.UserId == userId)
            .OrderBy(b => b.PositionSeconds)
            .Select(b => new BookmarkResponse(b.Id, b.PositionSeconds, b.Label, b.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<BookmarkResponse> CreateBookmarkAsync(
        Guid audiobookId, string userId, CreateBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PositionSeconds < 0)
        {
            throw new ValidationException("positionSeconds", "Position must be non-negative.");
        }

        var audiobookExists = await _db.Audiobooks
            .AnyAsync(a => a.Id == audiobookId, cancellationToken);

        if (!audiobookExists)
        {
            throw new NotFoundException(nameof(Audiobook), audiobookId);
        }

        var bookmark = new Bookmark
        {
            UserId = userId,
            AudiobookId = audiobookId,
            PositionSeconds = request.PositionSeconds,
            Label = request.Label?.Trim(),
        };

        _db.Bookmarks.Add(bookmark);
        await _db.SaveChangesAsync(cancellationToken);

        return new BookmarkResponse(bookmark.Id, bookmark.PositionSeconds, bookmark.Label, bookmark.CreatedAt);
    }

    public async Task DeleteBookmarkAsync(
        Guid audiobookId, Guid bookmarkId, string userId, CancellationToken cancellationToken = default)
    {
        var bookmark = await _db.Bookmarks
            .FirstOrDefaultAsync(
                b => b.Id == bookmarkId && b.AudiobookId == audiobookId && b.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Bookmark), bookmarkId);

        _db.Bookmarks.Remove(bookmark);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
