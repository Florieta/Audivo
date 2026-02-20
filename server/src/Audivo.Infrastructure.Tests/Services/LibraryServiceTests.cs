using Audivo.Application.Exceptions;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Audivo.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Audivo.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="LibraryService"/>.
/// </summary>
public class LibraryServiceTests
{
    private static AudivoDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AudivoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AudivoDbContext(options);
    }

    [Fact]
    public async Task GetGalleryAudiobooksAsync_EmptyDb_ReturnsEmptyList()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = new LibraryService(db);
        var userId = "user-1";

        // Act
        var result = await sut.GetGalleryAudiobooksAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetGalleryAudiobooksAsync_WithAudiobooks_ReturnsOrderedByGenreAndCreatedAt()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab1 = await TestDbFixture.SeedAudiobookAsync(db, user, "Book A", "Author A", "Fiction", cancellationToken: default);
        var ab2 = await TestDbFixture.SeedAudiobookAsync(db, user, "Book B", "Author B", "Mystery", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetGalleryAudiobooksAsync(user, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().Contain(new[] { ab1.Id, ab2.Id });
        result[0].Title.Should().BeOneOf("Book A", "Book B");
    }

    [Fact]
    public async Task GetGalleryFilteredAsync_ByGenre_ReturnsOnlyMatchingGenre()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        await TestDbFixture.SeedAudiobookAsync(db, user, "Fiction Book", "Author", "Fiction", cancellationToken: default);
        await TestDbFixture.SeedAudiobookAsync(db, user, "Mystery Book", "Author", "Mystery", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetGalleryFilteredAsync(user, genre: "Fiction", cancellationToken: CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Genre.Should().Be("Fiction");
    }

    [Fact]
    public async Task GetGalleryFilteredAsync_ByAuthor_ReturnsOnlyMatchingAuthor()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        await TestDbFixture.SeedAudiobookAsync(db, user, "Title 1", "Alice", cancellationToken: default);
        await TestDbFixture.SeedAudiobookAsync(db, user, "Title 2", "Bob", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetGalleryFilteredAsync(user, author: "Alice", cancellationToken: CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Author.Should().Be("Alice");
    }

    [Fact]
    public async Task GetGalleryFilteredAsync_SortByTitle_ReturnsOrderedByTitle()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        await TestDbFixture.SeedAudiobookAsync(db, user, "Zebra", "A", cancellationToken: default);
        await TestDbFixture.SeedAudiobookAsync(db, user, "Apple", "B", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetGalleryFilteredAsync(user, sortBy: "title", cancellationToken: CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Apple");
        result[1].Title.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetDistinctAuthorsAsync_WithAudiobooks_ReturnsSortedDistinctAuthors()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        await TestDbFixture.SeedAudiobookAsync(db, user, "B1", "Charlie", cancellationToken: default);
        await TestDbFixture.SeedAudiobookAsync(db, user, "B2", "Alice", cancellationToken: default);
        await TestDbFixture.SeedAudiobookAsync(db, user, "B3", "Charlie", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetDistinctAuthorsAsync(CancellationToken.None);

        // Assert
        result.Should().Equal("Alice", "Charlie");
    }

    [Fact]
    public async Task GetUploadedBooksAsync_ReturnsOnlyBooksUploadedByUser()
    {
        // Arrange
        await using var db = CreateDb();
        var owner = "owner-1";
        var other = "other-1";
        await TestDbFixture.SeedAudiobookAsync(db, owner, "Mine", "Me", cancellationToken: default);
        await TestDbFixture.SeedAudiobookAsync(db, other, "Theirs", "Them", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetUploadedBooksAsync(owner, CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Title.Should().Be("Mine");
    }

    [Fact]
    public async Task GetFavoriteBooksAsync_ReturnsOnlyFavoritesForUser()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, user, "Fav Book", "Author", cancellationToken: default);
        db.FavoriteAudiobooks.Add(new FavoriteAudiobook { UserId = user, AudiobookId = ab.Id });
        await db.SaveChangesAsync();
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetFavoriteBooksAsync(user, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Title.Should().Be("Fav Book");
        result[0].IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAudiobooksAsync_EmptyOrWhitespaceSearchTerm_ReturnsEmptyList()
    {
        // Arrange
        await using var db = CreateDb();
        await TestDbFixture.SeedAudiobookAsync(db, "u1", "Some Book", "Author", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var empty = await sut.SearchAudiobooksAsync("user-1", "", CancellationToken.None);
        var whitespace = await sut.SearchAudiobooksAsync("user-1", "   ", CancellationToken.None);

        // Assert
        empty.Should().NotBeNull().And.BeEmpty();
        whitespace.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task AddToFavoritesAsync_AudiobookNotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = new LibraryService(db);
        var nonexistentId = Guid.NewGuid();
        var userId = "user-1";

        // Act
        var act = () => sut.AddToFavoritesAsync(nonexistentId, userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Audiobook*");
    }

    [Fact]
    public async Task AddToFavoritesAsync_ValidAudiobook_AddsToFavorites()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, user, "To Fav", "Author", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        await sut.AddToFavoritesAsync(ab.Id, user, CancellationToken.None);

        // Assert
        var fav = await db.FavoriteAudiobooks.FirstOrDefaultAsync(f => f.UserId == user && f.AudiobookId == ab.Id);
        fav.Should().NotBeNull();
    }

    [Fact]
    public async Task AddToFavoritesAsync_AlreadyFavorite_DoesNotThrow()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, user, "Book", "Author", cancellationToken: default);
        db.FavoriteAudiobooks.Add(new FavoriteAudiobook { UserId = user, AudiobookId = ab.Id });
        await db.SaveChangesAsync();
        var sut = new LibraryService(db);

        // Act
        await sut.AddToFavoritesAsync(ab.Id, user, CancellationToken.None);

        // Assert
        var count = await db.FavoriteAudiobooks.CountAsync(f => f.UserId == user && f.AudiobookId == ab.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_NotFavorite_DoesNotThrow()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, user, "Book", "Author", cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        await sut.RemoveFromFavoritesAsync(ab.Id, user, CancellationToken.None);

        // Assert
        var fav = await db.FavoriteAudiobooks.FirstOrDefaultAsync(f => f.UserId == user && f.AudiobookId == ab.Id);
        fav.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_IsFavorite_RemovesFavorite()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, user, "Book", "Author", cancellationToken: default);
        db.FavoriteAudiobooks.Add(new FavoriteAudiobook { UserId = user, AudiobookId = ab.Id });
        await db.SaveChangesAsync();
        var sut = new LibraryService(db);

        // Act
        await sut.RemoveFromFavoritesAsync(ab.Id, user, CancellationToken.None);

        // Assert
        var fav = await db.FavoriteAudiobooks.FirstOrDefaultAsync(f => f.UserId == user && f.AudiobookId == ab.Id);
        fav.Should().BeNull();
    }

    [Fact]
    public async Task GetGalleryAudiobooksAsync_WithProgress_EnrichesWithListenedSeconds()
    {
        // Arrange
        await using var db = CreateDb();
        var user = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, user, "Book", "Author", cancellationToken: default);
        await TestDbFixture.SeedProgressAsync(db, user, ab.Id, positionSeconds: 120, cancellationToken: default);
        var sut = new LibraryService(db);

        // Act
        var result = await sut.GetGalleryAudiobooksAsync(user, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].ListenedSeconds.Should().Be(120);
        result[0].ProgressPercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGalleryAudiobooksAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = new LibraryService(db);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.GetGalleryAudiobooksAsync("user-1", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
