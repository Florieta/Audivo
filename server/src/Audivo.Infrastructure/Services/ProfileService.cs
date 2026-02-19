using Audivo.Application.DTOs.Profile;
using Audivo.Application.Exceptions;
using Audivo.Application.Interfaces;
using Audivo.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Audivo.Infrastructure.Services;

/// <summary>
/// Handles reading and updating the authenticated user's profile data and profile photo.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileStorageService;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileStorageService)
    {
        _userManager = userManager;
        _fileStorageService = fileStorageService;
    }

    /// <inheritdoc />
    public async Task<ProfileResponse> GetCurrentProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        return MapProfile(user);
    }

    /// <inheritdoc />
    public async Task<ProfileResponse> UpdateCurrentProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new ValidationException("firstName", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationException("lastName", "Last name is required.");
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        if (request.ProfileImage is not null)
        {
            var oldImage = user.ProfileImageUrl;
            user.ProfileImageUrl = await _fileStorageService.SaveFileAsync(request.ProfileImage, "profiles", cancellationToken);
            _fileStorageService.DeleteFile(oldImage);
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return MapProfile(user);
    }

    private static ProfileResponse MapProfile(ApplicationUser user) =>
        new(
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email ?? string.Empty,
            user.ProfileImageUrl,
            user.CreatedAt);
}
