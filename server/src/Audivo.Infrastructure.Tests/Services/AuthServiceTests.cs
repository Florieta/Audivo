using Audivo.Application.Configuration;
using Audivo.Application.DTOs.Auth;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Audivo.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Audivo.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="AuthService"/>.
/// </summary>
public class AuthServiceTests
{
    private static readonly JwtSettings JwtSettings = new()
    {
        Secret = "TestSecretKeyForJwtSigningThatIsLongEnough!",
        Issuer = "Test",
        Audience = "Test",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private static AudivoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudivoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AudivoDbContext(options);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task RegisterAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(
            userManager.Object,
            db,
            tokenService.Object,
            Options.Create(JwtSettings));

        // Act
        var act = () => sut.RegisterAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsValidationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@test.com",
            Password = "Pass123!",
            FirstName = "Jane",
            LastName = "Doe"
        };
        var existingUser = new ApplicationUser { Id = "existing-id", Email = request.Email, FirstName = "J", LastName = "D" };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("Email"));
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsResponseAndRefreshToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new@test.com",
            Password = "Pass123!",
            FirstName = "New",
            LastName = "User"
        };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        ApplicationUser? capturedUser = null;
        userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .Callback<ApplicationUser, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");
        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token-string");
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var (response, refreshToken) = await sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().Be("access-token");
        response.Email.Should().Be(request.Email);
        response.FirstName.Should().Be(request.FirstName);
        response.LastName.Should().Be(request.LastName);
        refreshToken.Should().Be("refresh-token-string");
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(request.Email);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_CreateFails_ThrowsValidationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new@test.com",
            Password = "short",
            FirstName = "A",
            LastName = "B"
        };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Too short." }));
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task LoginAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.LoginAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest { Email = "nobody@test.com", Password = "any" };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest { Email = "user@test.com", Password = "wrong" };
        var user = new ApplicationUser { Id = "u1", Email = request.Email, UserName = request.Email, FirstName = "A", LastName = "B" };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        userManager
            .Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsResponseAndRefreshToken()
    {
        // Arrange
        var request = new LoginRequest { Email = "user@test.com", Password = "CorrectPass1!" };
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Jane",
            LastName = "Doe"
        };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        userManager
            .Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        userManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("jwt-token");
        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var (response, refreshToken) = await sut.LoginAsync(request, CancellationToken.None);

        // Assert
        response.AccessToken.Should().Be("jwt-token");
        response.Email.Should().Be(request.Email);
        refreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsAuthenticationException()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.RefreshTokenAsync("invalid-token", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid refresh token*");
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ThrowsAuthenticationException()
    {
        // Arrange
        var user = new ApplicationUser { Id = "u1", Email = "u@t.com", UserName = "u@t.com", FirstName = "A", LastName = "B" };
        var storedToken = new RefreshToken
        {
            Token = "old-refresh",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        await using var db = CreateDbContext();
        db.RefreshTokens.Add(storedToken);
        await db.SaveChangesAsync();
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("replacement");
        tokenService.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("access");
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.RefreshTokenAsync("old-refresh", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*no longer valid*");
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesAndReturnsNewTokens()
    {
        // Arrange
        var user = new ApplicationUser { Id = "u1", Email = "u@t.com", UserName = "u@t.com", FirstName = "A", LastName = "B" };
        var storedToken = new RefreshToken
        {
            Token = "valid-refresh",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await using var db = CreateDbContext();
        db.RefreshTokens.Add(storedToken);
        await db.SaveChangesAsync();
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-string");
        tokenService.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("new-access");
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var (response, newRefreshToken) = await sut.RefreshTokenAsync("valid-refresh", CancellationToken.None);

        // Assert
        response.AccessToken.Should().Be("new-access");
        newRefreshToken.Should().Be("new-refresh-string");
        var oldInDb = await db.RefreshTokens.FindAsync(storedToken.Id);
        oldInDb!.RevokedAt.Should().NotBeNull();
        oldInDb.ReplacedByToken.Should().Be("new-refresh-string");
        var newInDb = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "new-refresh-string");
        newInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_InvalidToken_ThrowsAuthenticationException()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.RevokeTokenAsync("missing", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid refresh token*");
    }

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_SetsRevokedAt()
    {
        // Arrange
        var storedToken = new RefreshToken
        {
            Token = "to-revoke",
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await using var db = CreateDbContext();
        db.RefreshTokens.Add(storedToken);
        await db.SaveChangesAsync();
        var userManager = CreateUserManagerMock();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        await sut.RevokeTokenAsync("to-revoke", CancellationToken.None);

        // Assert
        var entity = await db.RefreshTokens.FindAsync(storedToken.Id);
        entity!.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange - simulate cancellation by having the first async call throw
        var request = new RegisterRequest
        {
            Email = "c@test.com",
            Password = "Pass1!",
            FirstName = "C",
            LastName = "D"
        };
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ThrowsAsync(new OperationCanceledException());
        await using var db = CreateDbContext();
        var tokenService = new Mock<ITokenService>();
        var sut = new AuthService(userManager.Object, db, tokenService.Object, Options.Create(JwtSettings));

        // Act
        var act = () => sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
