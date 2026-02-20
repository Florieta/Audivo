using System.Security.Claims;
using Audivo.Application.Configuration;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Audivo.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="TokenService"/>.
/// </summary>
public class TokenServiceTests
{
    private static readonly JwtSettings JwtSettings = new()
    {
        Secret = "ThisIsAVeryLongSecretKeyThatExceedsMinimumLengthForHmacSha256!",
        Issuer = "Audivo.Test",
        Audience = "Audivo.Test",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _sut = new TokenService(Options.Create(JwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_ValidUserAndRoles_ReturnsNonEmptyJwt()
    {
        // Arrange
        var user = CreateUser("user-1", "user@test.com", "Jane", "Doe");
        var roles = new List<string> { "User", "Admin" };

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3); // JWT has 3 segments
    }

    [Fact]
    public void GenerateAccessToken_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        ApplicationUser? user = null;
        var roles = new List<string>();

        // Act
        var act = () => _sut.GenerateAccessToken(user!, roles);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_EncodesUserIdInClaims()
    {
        // Arrange
        var userId = "user-123";
        var user = CreateUser(userId, "u@test.com", "A", "B");
        var roles = new List<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(userId);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_EncodesEmailAndNameInClaims()
    {
        // Arrange
        var user = CreateUser("id", "jane@audivo.com", "Jane", "Doe");
        var roles = new List<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.Email).Should().Be("jane@audivo.com");
        principal!.FindFirstValue(ClaimTypes.GivenName).Should().Be("Jane");
        principal!.FindFirstValue(ClaimTypes.Surname).Should().Be("Doe");
    }

    [Fact]
    public void GenerateAccessToken_WithRoles_EncodesRolesInClaims()
    {
        // Arrange
        var user = CreateUser("id", "a@b.com", "A", "B");
        var roles = new List<string> { "User", "Admin" };

        // Act
        var token = _sut.GenerateAccessToken(user, roles);
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        var roleClaims = principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().BeEquivalentTo(new[] { "User", "Admin" });
    }

    [Fact]
    public void GenerateRefreshToken_Called_ReturnsNonEmptyBase64String()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        Convert.FromBase64String(token).Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_CalledMultipleTimes_ReturnsDifferentTokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var user = CreateUser("id", "e@mail.com", "F", "L");
        var token = _sut.GenerateAccessToken(user, new List<string>());

        // Act
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_InvalidToken_ReturnsNull()
    {
        // Act
        var principal = _sut.GetPrincipalFromExpiredToken("invalid.jwt.token");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_EmptyString_ReturnsNull()
    {
        // Act
        var principal = _sut.GetPrincipalFromExpiredToken("");

        // Assert
        principal.Should().BeNull();
    }

    private static ApplicationUser CreateUser(string id, string email, string firstName, string lastName)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };
    }
}
