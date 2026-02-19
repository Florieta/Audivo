using Audivo.Application.DTOs.Player;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

/// <summary>
/// Manages audio playback state: fetching player details, persisting listening progress, and managing bookmarks.
/// </summary>
[Authorize]
public class PlayerController : ApiControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    /// <summary>
    /// Get full player state for an audiobook (details, progress, bookmarks).
    /// </summary>
    [HttpGet("{audiobookId:guid}")]
    public async Task<ActionResult<PlayerStateResponse>> GetPlayerState(
        Guid audiobookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var state = await _playerService.GetPlayerStateAsync(audiobookId, userId, cancellationToken);
        return Ok(state);
    }

    /// <summary>
    /// Save the current playback position.
    /// </summary>
    [HttpPut("{audiobookId:guid}/progress")]
    public async Task<IActionResult> SaveProgress(
        Guid audiobookId,
        [FromBody] SaveProgressRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _playerService.SaveProgressAsync(audiobookId, userId, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get all bookmarks for an audiobook.
    /// </summary>
    [HttpGet("{audiobookId:guid}/bookmarks")]
    public async Task<ActionResult<IReadOnlyList<BookmarkResponse>>> GetBookmarks(
        Guid audiobookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var bookmarks = await _playerService.GetBookmarksAsync(audiobookId, userId, cancellationToken);
        return Ok(bookmarks);
    }

    /// <summary>
    /// Create a bookmark at a specific timestamp.
    /// </summary>
    [HttpPost("{audiobookId:guid}/bookmarks")]
    public async Task<ActionResult<BookmarkResponse>> CreateBookmark(
        Guid audiobookId,
        [FromBody] CreateBookmarkRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var bookmark = await _playerService.CreateBookmarkAsync(audiobookId, userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetBookmarks), new { audiobookId }, bookmark);
    }

    /// <summary>
    /// Delete a specific bookmark.
    /// </summary>
    [HttpDelete("{audiobookId:guid}/bookmarks/{bookmarkId:guid}")]
    public async Task<IActionResult> DeleteBookmark(
        Guid audiobookId, Guid bookmarkId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _playerService.DeleteBookmarkAsync(audiobookId, bookmarkId, userId, cancellationToken);
        return NoContent();
    }
}
