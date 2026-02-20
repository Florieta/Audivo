using System.IO;
using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Infrastructure.Persistence;
using Audivo.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Audivo.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="AudiobookService"/>.
/// </summary>
public class AudiobookServiceTests
{
    private static AudivoDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AudivoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AudivoDbContext(options);
    }

    private static FileUpload CreateFakeFileUpload(string fileName = "test.mp3", string contentType = "audio/mpeg")
    {
        return new FileUpload(new MemoryStream(), fileName, contentType, 1024);
    }

    [Fact]
    public async Task GetUserAudiobooksAsync_NoBooks_ReturnsEmptyList()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var userId = "user-1";

        // Act
        var result = await sut.GetUserAudiobooksAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetUserAudiobooksAsync_WithUploadedBooks_ReturnsOnlyUserBooks()
    {
        // Arrange
        await using var db = CreateDb();
        var owner = "owner-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, owner, "My Book", "Author", cancellationToken: default);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);

        // Act
        var result = await sut.GetUserAudiobooksAsync(owner, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(ab.Id);
        result[0].Title.Should().Be("My Book");
    }

    [Fact]
    public async Task CreateAudiobookAsync_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new CreateAudiobookRequest("", "Author", null, null, CreateFakeFileUpload());

        // Act
        var act = () => sut.CreateAudiobookAsync(request, "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("title"));
    }

    [Fact]
    public async Task CreateAudiobookAsync_EmptyAuthor_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new CreateAudiobookRequest("Title", "  ", null, null, CreateFakeFileUpload());

        // Act
        var act = () => sut.CreateAudiobookAsync(request, "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("author"));
    }

    [Fact]
    public async Task CreateAudiobookAsync_InvalidGenre_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new CreateAudiobookRequest("Title", "Author", "InvalidGenre", null, CreateFakeFileUpload());

        // Act
        var act = () => sut.CreateAudiobookAsync(request, "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("genre"));
    }

    [Fact]
    public async Task CreateAudiobookAsync_ValidRequest_CreatesAudiobookAndReturnsResponse()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(x => x.SaveFileAsync(It.IsAny<FileUpload>(), "audio", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/audio/ch1.mp3");
        fileStorage.Setup(x => x.SaveFileAsync(It.IsAny<FileUpload>(), "covers", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/covers/cover.jpg");
        var sut = new AudiobookService(db, fileStorage.Object);
        var userId = "user-1";
        var request = new CreateAudiobookRequest(
            "  My Title  ",
            "  My Author  ",
            "Fiction",
            CreateFakeFileUpload("cover.jpg", "image/jpeg"),
            CreateFakeFileUpload("audio.mp3"));

        // Act
        var result = await sut.CreateAudiobookAsync(request, userId, CancellationToken.None);

        // Assert
        result.Title.Should().Be("My Title");
        result.Author.Should().Be("My Author");
        result.Genre.Should().Be("Fiction");
        result.AudioFileUrl.Should().Be("https://storage/audio/ch1.mp3");
        var entity = await db.Audiobooks.Include(a => a.Chapters).FirstOrDefaultAsync(a => a.Id == result.Id);
        entity.Should().NotBeNull();
        entity!.UploadedByUserId.Should().Be(userId);
        entity.Chapters.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAudiobookAsync_AudioUploadFails_DeletesCoverAndThrows()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(x => x.SaveFileAsync(It.IsAny<FileUpload>(), "covers", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/cover.jpg");
        fileStorage.Setup(x => x.SaveFileAsync(It.IsAny<FileUpload>(), "audio", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Upload failed"));
        fileStorage.Setup(x => x.DeleteFileAsync("https://storage/cover.jpg", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new CreateAudiobookRequest("T", "A", null, CreateFakeFileUpload(), CreateFakeFileUpload());

        // Act
        var act = () => sut.CreateAudiobookAsync(request, "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        fileStorage.Verify(x => x.DeleteFileAsync("https://storage/cover.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAudiobookAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new UpdateAudiobookRequest("New Title", null, null, null, null);

        // Act
        var act = () => sut.UpdateAudiobookAsync(Guid.NewGuid(), request, "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Audiobook*");
    }

    [Fact]
    public async Task UpdateAudiobookAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var db = CreateDb();
        var owner = "owner-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, owner, "Book", "Author", cancellationToken: default);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new UpdateAudiobookRequest("Hacked", null, null, null, null);

        // Act
        var act = () => sut.UpdateAudiobookAsync(ab.Id, request, "other-user", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateAudiobookAsync_ValidPartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Old Title", "Old Author", "Fiction", cancellationToken: default);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new UpdateAudiobookRequest("New Title", null, null, null, null);

        // Act
        var result = await sut.UpdateAudiobookAsync(ab.Id, request, userId, CancellationToken.None);

        // Assert
        result.Title.Should().Be("New Title");
        result.Author.Should().Be("Old Author");
        var entity = await db.Audiobooks.FindAsync(ab.Id);
        entity!.Title.Should().Be("New Title");
        entity.Author.Should().Be("Old Author");
    }

    [Fact]
    public async Task UpdateAudiobookAsync_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Title", "Author", cancellationToken: default);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new UpdateAudiobookRequest("  ", null, null, null, null);

        // Act
        var act = () => sut.UpdateAudiobookAsync(ab.Id, request, userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("title"));
    }

    [Fact]
    public async Task DeleteAudiobookAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);

        // Act
        var act = () => sut.DeleteAudiobookAsync(Guid.NewGuid(), "user-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAudiobookAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var db = CreateDb();
        var ab = await TestDbFixture.SeedAudiobookAsync(db, "owner", "Book", "Author", cancellationToken: default);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new AudiobookService(db, fileStorage.Object);

        // Act
        var act = () => sut.DeleteAudiobookAsync(ab.Id, "other-user", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteAudiobookAsync_Owner_DeletesAudiobookAndCleansFiles()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = "user-1";
        var ab = await TestDbFixture.SeedAudiobookAsync(db, userId, "Book", "Author", cancellationToken: default);
        var coverUrl = ab.CoverImageUrl;
        var audioUrl = ab.Chapters.First().AudioFileUrl;
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new AudiobookService(db, fileStorage.Object);

        // Act
        await sut.DeleteAudiobookAsync(ab.Id, userId, CancellationToken.None);

        // Assert
        var entity = await db.Audiobooks.FindAsync(ab.Id);
        entity.Should().BeNull();
        fileStorage.Verify(x => x.DeleteFileAsync(coverUrl, It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(x => x.DeleteFileAsync(audioUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAudiobookAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var db = CreateDb();
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(x => x.SaveFileAsync(It.IsAny<FileUpload>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var sut = new AudiobookService(db, fileStorage.Object);
        var request = new CreateAudiobookRequest("T", "A", null, null, CreateFakeFileUpload());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.CreateAudiobookAsync(request, "user-1", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
