using Audivo.Application.DTOs.Player;
using Audivo.Application.Exceptions;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Audivo.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Audivo.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="PlayerService"/>.
/// </summary>
public class PlayerServiceTests
{
    private static AudivoDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AudivoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AudivoDbContext(options);
    }

    [Fact]
    public async Task GetPlayerStateAsync_AudiobookNotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = new PlayerService(db);
        var audiobookId = Guid.NewGuid();
        var userId = "user-1";

        // Act
        var act = () => sut.GetPlayerStateAsync(audiobookId, userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Audiobook*");
    }

    [Fact]
    public async Task GetPlayerStateAsync_ValidAudiobook_ReturnsPlayerStateWithProgressAndBookmarks()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Player Book", "Author", "Fiction", cancellationToken: default);
        await TestDbFixture.SeedProgressAsync(db, userId, ab.Id, positionSeconds: 300, cancellationToken: default);
        var sut = new PlayerService(db);

        // Act
        var result = await sut.GetPlayerStateAsync(ab.Id, userId, CancellationToken.None);

        // Assert
        result.AudiobookId.Should().Be(ab.Id);
        result.Title.Should().Be("Player Book");
        result.Author.Should().Be("Author");
        result.LastPositionSeconds.Should().Be(300);
        result.Bookmarks.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetPlayerStateAsync_NoProgress_ReturnsZeroPosition()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);

        // Act
        var result = await sut.GetPlayerStateAsync(ab.Id, userId, CancellationToken.None);

        // Assert
        result.LastPositionSeconds.Should().Be(0);
        result.LastListenedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetPlayerStateAsync_WithBookmarks_ReturnsBookmarksOrderedByPosition()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        db.Bookmarks.Add(new Bookmark { UserId = userId, AudiobookId = ab.Id, PositionSeconds = 600, Label = "Ch2" });
        db.Bookmarks.Add(new Bookmark { UserId = userId, AudiobookId = ab.Id, PositionSeconds = 120, Label = "Ch1" });
        await db.SaveChangesAsync();
        var sut = new PlayerService(db);

        // Act
        var result = await sut.GetPlayerStateAsync(ab.Id, userId, CancellationToken.None);

        // Assert
        result.Bookmarks.Should().HaveCount(2);
        result.Bookmarks[0].PositionSeconds.Should().Be(120);
        result.Bookmarks[1].PositionSeconds.Should().Be(600);
    }

    [Fact]
    public async Task SaveProgressAsync_NegativePosition_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "u1", "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);
        var request = new SaveProgressRequest(-1);

        // Act
        var act = () => sut.SaveProgressAsync(ab.Id, "user-1", request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("positionSeconds"));
    }

    [Fact]
    public async Task SaveProgressAsync_ZeroOrNegativeTotalDuration_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "u1", "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);
        var request = new SaveProgressRequest(100, 0);

        // Act
        var act = () => sut.SaveProgressAsync(ab.Id, "user-1", request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("totalDurationSeconds"));
    }

    [Fact]
    public async Task SaveProgressAsync_AudiobookNotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = new PlayerService(db);
        var request = new SaveProgressRequest(100);

        // Act
        var act = () => sut.SaveProgressAsync(Guid.NewGuid(), "user-1", request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SaveProgressAsync_NoExistingProgress_CreatesNewProgress()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);
        var request = new SaveProgressRequest(250.5);

        // Act
        await sut.SaveProgressAsync(ab.Id, userId, request, CancellationToken.None);

        // Assert
        var progress = await db.ListeningProgressRecords.FirstOrDefaultAsync(
            p => p.AudiobookId == ab.Id && p.UserId == userId);
        progress.Should().NotBeNull();
        progress!.PositionInChapter.TotalSeconds.Should().Be(250.5);
    }

    [Fact]
    public async Task SaveProgressAsync_ExistingProgress_UpdatesPosition()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        await TestDbFixture.SeedProgressAsync(db, userId, ab.Id, positionSeconds: 100, cancellationToken: default);
        var sut = new PlayerService(db);
        var request = new SaveProgressRequest(500);

        // Act
        await sut.SaveProgressAsync(ab.Id, userId, request, CancellationToken.None);

        // Assert
        var progress = await db.ListeningProgressRecords.FirstAsync(
            p => p.AudiobookId == ab.Id && p.UserId == userId);
        progress.PositionInChapter.TotalSeconds.Should().Be(500);
    }

    [Fact]
    public async Task GetBookmarksAsync_NoBookmarks_ReturnsEmptyList()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "u1", "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);

        // Act
        var result = await sut.GetBookmarksAsync(ab.Id, "user-1", CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task CreateBookmarkAsync_NegativePosition_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "u1", "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);
        var request = new CreateBookmarkRequest(-1, "Bad");

        // Act
        var act = () => sut.CreateBookmarkAsync(ab.Id, "user-1", request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("positionSeconds"));
    }

    [Fact]
    public async Task CreateBookmarkAsync_AudiobookNotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = new PlayerService(db);
        var request = new CreateBookmarkRequest(120, "Note");

        // Act
        var act = () => sut.CreateBookmarkAsync(Guid.NewGuid(), "user-1", request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateBookmarkAsync_ValidRequest_CreatesBookmarkAndReturnsResponse()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);
        var request = new CreateBookmarkRequest(300.5, "  Chapter 2 start  ");

        // Act
        var result = await sut.CreateBookmarkAsync(ab.Id, userId, request, CancellationToken.None);

        // Assert
        result.PositionSeconds.Should().Be(300.5);
        result.Label.Should().Be("Chapter 2 start");
        result.Id.Should().NotBeEmpty();
        var entity = await db.Bookmarks.FirstOrDefaultAsync(b => b.Id == result.Id);
        entity.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteBookmarkAsync_BookmarkNotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "u1", "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);

        // Act
        var act = () => sut.DeleteBookmarkAsync(ab.Id, Guid.NewGuid(), "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Bookmark*");
    }

    [Fact]
    public async Task DeleteBookmarkAsync_ValidBookmark_RemovesBookmark()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        var bookmark = new Bookmark { UserId = userId, AudiobookId = ab.Id, PositionSeconds = 100, Label = "To delete" };
        db.Bookmarks.Add(bookmark);
        await db.SaveChangesAsync();
        var sut = new PlayerService(db);

        // Act
        await sut.DeleteBookmarkAsync(ab.Id, bookmark.Id, userId, CancellationToken.None);

        // Assert
        var entity = await db.Bookmarks.FindAsync(bookmark.Id);
        entity.Should().BeNull();
    }

    [Fact]
    public async Task GetPlayerStateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "u1", "Book", "Author", cancellationToken: default);
        var sut = new PlayerService(db);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.GetPlayerStateAsync(ab.Id, "user-1", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
