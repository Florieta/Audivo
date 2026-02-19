using Audivo.Application.Configuration;
using Audivo.Application.DTOs.Auth;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Audivo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Audivo.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AudivoDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AudivoDbContext dbContext,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<(AuthResponse Response, string RefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new Application.Exceptions.ValidationException("Email", "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new Application.Exceptions.ValidationException(errors);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileImageUrl = user.ProfileImageUrl
        };

        return (response, refreshToken);
    }

    public async Task<(AuthResponse Response, string RefreshToken)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new AuthenticationException("Invalid email or password.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileImageUrl = user.ProfileImageUrl
        };

        return (response, refreshToken);
    }

    public async Task<(AuthResponse Response, string NewRefreshToken)> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken)
            ?? throw new AuthenticationException("Invalid refresh token.");

        if (!storedToken.IsActive)
        {
            throw new AuthenticationException("Refresh token is no longer valid.");
        }

        // Rotate: revoke old token, issue new one
        storedToken.RevokedAt = DateTime.UtcNow;
        var newRefreshTokenString = _tokenService.GenerateRefreshToken();
        storedToken.ReplacedByToken = newRefreshTokenString;

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenString,
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = storedToken.User;
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileImageUrl = user.ProfileImageUrl
        };

        return (response, newRefreshTokenString);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken)
            ?? throw new AuthenticationException("Invalid refresh token.");

        if (!storedToken.IsActive)
        {
            throw new AuthenticationException("Token is already revoked or expired.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> CreateRefreshTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshTokenString;
    }
}
