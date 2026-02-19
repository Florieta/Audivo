using System.Security.Claims;
using Audivo.Application.DTOs.Library;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

[Authorize]
public class LibraryController : ApiControllerBase
{
    private readonly ILibraryService _libraryService;

    public LibraryController(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    [HttpGet("gallery")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetGallery(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetGalleryAudiobooksAsync(userId, cancellationToken);
        return Ok(books);
    }

    [HttpGet("uploaded")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetUploaded(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetUploadedBooksAsync(userId, cancellationToken);
        return Ok(books);
    }

    [HttpGet("favorites")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetFavorites(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetFavoriteBooksAsync(userId, cancellationToken);
        return Ok(books);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.SearchAudiobooksAsync(userId, query, cancellationToken);
        return Ok(books);
    }

    [HttpPost("favorites/{audiobookId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid audiobookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _libraryService.AddToFavoritesAsync(audiobookId, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("favorites/{audiobookId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid audiobookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _libraryService.RemoveFromFavoritesAsync(audiobookId, userId, cancellationToken);
        return NoContent();
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();
}
