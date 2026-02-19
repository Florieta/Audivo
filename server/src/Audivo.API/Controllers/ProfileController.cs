using System.Security.Claims;
using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.DTOs.Profile;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

[Authorize]
public sealed class ProfileController : ApiControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await _profileService.GetCurrentProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ProfileResponse>> UpdateCurrentProfile(
        [FromForm] UpdateProfileForm form,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        FileUpload? profileUpload = null;
        if (form.ProfileImage is not null)
        {
            profileUpload = new FileUpload(
                form.ProfileImage.OpenReadStream(),
                form.ProfileImage.FileName,
                form.ProfileImage.ContentType,
                form.ProfileImage.Length);
        }

        var request = new UpdateProfileRequest(form.FirstName, form.LastName, profileUpload);
        var profile = await _profileService.UpdateCurrentProfileAsync(userId, request, cancellationToken);
        return Ok(profile);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();
}

public sealed record UpdateProfileForm(string FirstName, string LastName, IFormFile? ProfileImage);
