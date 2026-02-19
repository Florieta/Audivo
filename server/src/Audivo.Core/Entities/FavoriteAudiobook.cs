namespace Audivo.Core.Entities;

/// <summary>
/// Represents an audiobook marked as a favourite by a user.
/// </summary>
public class FavoriteAudiobook : BaseEntity
{
    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid AudiobookId { get; set; }

    public Audiobook Audiobook { get; set; } = null!;

    public DateTime AddedAt { get; init; } = DateTime.UtcNow;
}
