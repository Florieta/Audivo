using Audivo.Application.Configuration;
using Audivo.Application.DTOs.Auth;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Audivo.API.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, IOptions<JwtSettings> jwtSettings)
    {
        _authService = authService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var (response, refreshToken) = await _authService.RegisterAsync(request, cancellationToken);
        SetRefreshTokenCookie(refreshToken);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var (response, refreshToken) = await _authService.LoginAsync(request, cancellationToken);
        SetRefreshTokenCookie(refreshToken);
        return Ok(response);
    }

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
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
