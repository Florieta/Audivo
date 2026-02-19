using Audivo.Application.DTOs.Library;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

/// <summary>
/// Provides library browsing, favourites management, and audiobook search for the authenticated user.
/// </summary>
[Authorize]
public class LibraryController : ApiControllerBase
{
    private readonly ILibraryService _libraryService;

    public LibraryController(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    /// <summary>Returns the full public audiobook catalog enriched with the user's progress and favourite flags.</summary>
    [HttpGet("gallery")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetGallery(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetGalleryAudiobooksAsync(userId, cancellationToken);
        return Ok(books);
    }

    /// <summary>Returns gallery audiobooks filtered by genre, author, and sorted by the specified criteria.</summary>
    [HttpGet("gallery/filtered")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetGalleryFiltered(
        [FromQuery] string? genre,
        [FromQuery] string? author,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetGalleryFilteredAsync(userId, genre, author, sortBy, cancellationToken);
        return Ok(books);
    }

    /// <summary>Returns a distinct sorted list of all audiobook authors for filter dropdowns.</summary>
    [HttpGet("gallery/authors")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDistinctAuthors(
        CancellationToken cancellationToken)
    {
        var authors = await _libraryService.GetDistinctAuthorsAsync(cancellationToken);
        return Ok(authors);
    }

    /// <summary>Returns audiobooks uploaded by the authenticated user, enriched with progress data.</summary>
    [HttpGet("uploaded")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetUploaded(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetUploadedBooksAsync(userId, cancellationToken);
        return Ok(books);
    }

    /// <summary>Returns the user's favourite audiobooks ordered by when they were added.</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> GetFavorites(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.GetFavoriteBooksAsync(userId, cancellationToken);
        return Ok(books);
    }

    /// <summary>Searches all audiobooks by title, author, or genre. Returns an empty list when the query is blank.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<LibraryAudiobookResponse>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var books = await _libraryService.SearchAudiobooksAsync(userId, query, cancellationToken);
        return Ok(books);
    }

    /// <summary>Adds the specified audiobook to the user's favourites. Idempotent — no error if already a favourite.</summary>
    [HttpPost("favorites/{audiobookId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid audiobookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _libraryService.AddToFavoritesAsync(audiobookId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Removes the specified audiobook from the user's favourites. Idempotent — no error if not a favourite.</summary>
    [HttpDelete("favorites/{audiobookId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid audiobookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _libraryService.RemoveFromFavoritesAsync(audiobookId, userId, cancellationToken);
        return NoContent();
    }
}
