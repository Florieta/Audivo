using Audivo.Application.Configuration;
using Audivo.Application.DTOs.Auth;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Audivo.API.Controllers;

/// <summary>
/// Handles user registration, login, access-token refresh, and sign-out.
/// Refresh tokens are transported exclusively via an HttpOnly cookie to prevent XSS exposure.
/// </summary>
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IOptions<JwtSettings> jwtSettings,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _jwtSettings = jwtSettings.Value;
        _environment = environment;
    }

    /// <summary>Registers a new user account and returns an access token with a refresh-token cookie.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var (response, refreshToken) = await _authService.RegisterAsync(request, cancellationToken);
        SetRefreshTokenCookie(refreshToken);
        return Ok(response);
    }

    /// <summary>Authenticates an existing user and returns an access token with a refreshed cookie.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var (response, refreshToken) = await _authService.LoginAsync(request, cancellationToken);
        SetRefreshTokenCookie(refreshToken);
        return Ok(response);
    }

    /// <summary>
    /// Uses the refresh-token cookie to issue a new access token and rotate the refresh token.
    /// Returns 401 if no cookie is present or the token is invalid/expired.
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token is required." });
        }

        var (response, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        SetRefreshTokenCookie(newRefreshToken);
        return Ok(response);
    }

    /// <summary>Revokes the current refresh-token cookie, effectively signing the user out.</summary>
    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        await _authService.RevokeTokenAsync(refreshToken, cancellationToken);

        // Delete the cookie using the same options it was set with so the browser removes it correctly.
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = _environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict
        });

        return NoContent();
    }

    /// <summary>
    /// Sets the refresh token as an HttpOnly cookie.
    /// In development the cookie uses <c>SameSite=None</c> to allow cross-scheme requests from Vite's dev server.
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var sameSite = _environment.IsDevelopment()
            ? SameSiteMode.None
            : SameSiteMode.Strict;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
