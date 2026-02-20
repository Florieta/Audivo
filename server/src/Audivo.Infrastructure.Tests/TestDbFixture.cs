using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Audivo.Infrastructure.Tests;

/// <summary>
/// Provides an in-memory <see cref="AudivoDbContext"/> for unit testing services that depend on persistence.
/// </summary>
public static class TestDbFixture
{
    public static AudivoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AudivoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AudivoDbContext(options);
    }

    /// <summary>
    /// Seeds an audiobook with one chapter for library/player tests.
    /// </summary>
    public static async Task<Audiobook> SeedAudiobookAsync(
        AudivoDbContext db,
        string uploadedByUserId,
        string title = "Test Book",
        string author = "Test Author",
        string? genre = "Fiction",
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var audiobook = new Audiobook
        {
            Title = title,
            Author = author,
            Genre = genre,
            Description = description ?? "Description",
            CoverImageUrl = "https://cover.example/1.jpg",
            TotalDuration = TimeSpan.FromSeconds(3600),
            UploadedByUserId = uploadedByUserId
        };
        var chapter = new Chapter
        {
            Title = "Chapter 1",
            OrderIndex = 1,
            AudioFileUrl = "https://audio.example/ch1.mp3",
            AudiobookId = audiobook.Id
        };
        audiobook.Chapters.Add(chapter);
        db.Audiobooks.Add(audiobook);
        await db.SaveChangesAsync(cancellationToken);
        return audiobook;
    }

    /// <summary>
    /// Seeds a listening progress record.
    /// </summary>
    public static async Task<ListeningProgress> SeedProgressAsync(
        AudivoDbContext db,
        string userId,
        Guid audiobookId,
        double positionSeconds = 0,
        CancellationToken cancellationToken = default)
    {
        var progress = new ListeningProgress
        {
            UserId = userId,
            AudiobookId = audiobookId,
            PositionInChapter = TimeSpan.FromSeconds(positionSeconds),
            LastListenedAt = DateTime.UtcNow
        };
        db.ListeningProgressRecords.Add(progress);
        await db.SaveChangesAsync(cancellationToken);
        return progress;
    }
}
