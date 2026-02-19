using Microsoft.AspNetCore.Identity;

namespace Audivo.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserLibrary> UserLibraries { get; set; } = [];

    public ICollection<ListeningProgress> ListeningProgressRecords { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<Audiobook> UploadedAudiobooks { get; set; } = [];

    public ICollection<Bookmark> Bookmarks { get; set; } = [];

    public ICollection<FavoriteAudiobook> FavoriteAudiobooks { get; set; } = [];
}
