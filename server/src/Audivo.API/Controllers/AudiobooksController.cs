using System.ComponentModel.DataAnnotations;
using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

/// <summary>
/// Manages the lifecycle of audiobooks owned by the authenticated user.
/// All endpoints require authentication.
/// </summary>
[Authorize]
public class AudiobooksController : ApiControllerBase
{
    private readonly IAudiobookService _audiobookService;

    public AudiobooksController(IAudiobookService audiobookService)
    {
        _audiobookService = audiobookService;
    }

    /// <summary>Returns all audiobooks uploaded by the authenticated user.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AudiobookResponse>>> GetMyAudiobooks(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var audiobooks = await _audiobookService.GetUserAudiobooksAsync(userId, cancellationToken);
        return Ok(audiobooks);
    }

    /// <summary>Uploads a new audiobook with metadata, optional cover image, and required audio file.</summary>
    [HttpPost]
    [RequestSizeLimit(510 * 1024 * 1024)] // 510 MB overall form limit
    public async Task<ActionResult<AudiobookResponse>> Create(
        [FromForm] CreateAudiobookFormModel model,
        CancellationToken cancellationToken)
    {
        var request = new CreateAudiobookRequest(
            model.Title,
            model.Author,
            model.Genre,
            model.CoverImage is not null ? ToFileUpload(model.CoverImage) : null,
            ToFileUpload(model.AudioFile));

        var userId = GetUserId();
        var result = await _audiobookService.CreateAudiobookAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetMyAudiobooks), new { }, result);
    }

    /// <summary>Updates metadata or replaces the cover/audio files of an existing audiobook owned by the authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [RequestSizeLimit(510 * 1024 * 1024)]
    public async Task<ActionResult<AudiobookResponse>> Update(
        Guid id,
        [FromForm] UpdateAudiobookFormModel model,
        CancellationToken cancellationToken)
    {
        var request = new UpdateAudiobookRequest(
            model.Title,
            model.Author,
            model.Genre,
            model.CoverImage is not null ? ToFileUpload(model.CoverImage) : null,
            model.AudioFile is not null ? ToFileUpload(model.AudioFile) : null);

        var userId = GetUserId();
        var result = await _audiobookService.UpdateAudiobookAsync(id, request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Permanently deletes an audiobook and its associated files. Only the uploader may delete their own audiobooks.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _audiobookService.DeleteAudiobookAsync(id, userId, cancellationToken);
        return NoContent();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static FileUpload ToFileUpload(IFormFile file) =>
        new(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
}

// Form models live in the API layer so IFormFile never enters the Application layer.

/// <summary>Form binding model for creating a new audiobook.</summary>
public sealed class CreateAudiobookFormModel
{
    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Author { get; set; }

    public string? Genre { get; set; }

    public IFormFile? CoverImage { get; set; }

    [Required]
    public required IFormFile AudioFile { get; set; }
}

/// <summary>Form binding model for partially updating an existing audiobook. All fields are optional.</summary>
public sealed class UpdateAudiobookFormModel
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Genre { get; set; }
    public IFormFile? CoverImage { get; set; }
    public IFormFile? AudioFile { get; set; }
}
