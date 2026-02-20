using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.DTOs.Profile;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Audivo.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="ProfileService"/>.
/// </summary>
public class ProfileServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetCurrentProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "missing-user-id";
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);

        // Act
        var act = () => sut.GetCurrentProfileAsync(userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCurrentProfileAsync_ValidUserId_ReturnsProfileResponse()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "jane@test.com",
            UserName = "jane@test.com",
            FirstName = "Jane",
            LastName = "Doe",
            ProfileImageUrl = null,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);

        // Act
        var result = await sut.GetCurrentProfileAsync(userId, CancellationToken.None);

        // Assert
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
        result.FullName.Should().Be("Jane Doe");
        result.Email.Should().Be("jane@test.com");
        result.ProfileImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_EmptyFirstName_ThrowsValidationException()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, FirstName = "A", LastName = "B", Email = "a@b.com", UserName = "a@b.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);
        var request = new UpdateProfileRequest("", "Last", null);

        // Act
        var act = () => sut.UpdateCurrentProfileAsync(userId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("firstName"));
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_EmptyLastName_ThrowsValidationException()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser { Id = userId, FirstName = "A", LastName = "B", Email = "a@b.com", UserName = "a@b.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);
        var request = new UpdateProfileRequest("First", "  ", null);

        // Act
        var act = () => sut.UpdateCurrentProfileAsync(userId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("lastName"));
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "missing";
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);
        var request = new UpdateProfileRequest("First", "Last", null);

        // Act
        var act = () => sut.UpdateCurrentProfileAsync(userId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_ValidRequestWithoutImage_UpdatesNameAndReturnsProfile()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "u@t.com",
            UserName = "u@t.com",
            FirstName = "Old",
            LastName = "Name"
        };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);
        var request = new UpdateProfileRequest("  NewFirst  ", "  NewLast  ", null);

        // Act
        var result = await sut.UpdateCurrentProfileAsync(userId, request, CancellationToken.None);

        // Assert
        result.FirstName.Should().Be("NewFirst");
        result.LastName.Should().Be("NewLast");
        result.FullName.Should().Be("NewFirst NewLast");
        user.FirstName.Should().Be("NewFirst");
        user.LastName.Should().Be("NewLast");
        fileStorage.Verify(x => x.SaveFileAsync(It.IsAny<FileUpload>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_WithNewProfileImage_SavesImageAndDeletesOld()
    {
        // Arrange
        var userId = "user-1";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "u@t.com",
            UserName = "u@t.com",
            FirstName = "A",
            LastName = "B",
            ProfileImageUrl = "https://old-url/photo.jpg"
        };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(x => x.SaveFileAsync(It.IsAny<FileUpload>(), "profiles", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://new-url/profile.jpg");
        fileStorage.Setup(x => x.DeleteFileAsync("https://old-url/photo.jpg", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new ProfileService(userManager.Object, fileStorage.Object);
        var profileImage = new FileUpload(new MemoryStream(), "photo.jpg", "image/jpeg", 100);
        var request = new UpdateProfileRequest("A", "B", profileImage);

        // Act
        var result = await sut.UpdateCurrentProfileAsync(userId, request, CancellationToken.None);

        // Assert
        result.ProfileImageUrl.Should().Be("https://new-url/profile.jpg");
        user.ProfileImageUrl.Should().Be("https://new-url/profile.jpg");
        fileStorage.Verify(x => x.SaveFileAsync(profileImage, "profiles", It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(x => x.DeleteFileAsync("https://old-url/photo.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentProfileAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new OperationCanceledException());
        var fileStorage = new Mock<IFileStorageService>();
        var sut = new ProfileService(userManager.Object, fileStorage.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.GetCurrentProfileAsync("user-1", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
