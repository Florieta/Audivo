using Audivo.Application.DTOs.Audiobooks;
using Audivo.Application.DTOs.Profile;
using Audivo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audivo.API.Controllers;

/// <summary>
/// Allows the authenticated user to view and update their own profile, including uploading a profile photo.
/// </summary>
[Authorize]
public sealed class ProfileController : ApiControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>Returns the authenticated user's profile information.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await _profileService.GetCurrentProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>Updates the authenticated user's display name and optionally replaces their profile photo.</summary>
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
}

/// <summary>Form binding model for updating user profile details and optional profile image upload.</summary>
public sealed record UpdateProfileForm(
    [property: System.ComponentModel.DataAnnotations.Required] string FirstName,
    [property: System.ComponentModel.DataAnnotations.Required] string LastName,
    IFormFile? ProfileImage);
