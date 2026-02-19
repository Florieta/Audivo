namespace Audivo.Core.Entities;

/// <summary>
/// Tracks a user's listening position within an audiobook, enabling resume-from-last-position.
/// </summary>
public class ListeningProgress : BaseEntity
{
    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid AudiobookId { get; set; }

    public Audiobook Audiobook { get; set; } = null!;

    public Guid? CurrentChapterId { get; set; }

    public Chapter? CurrentChapter { get; set; }

    public TimeSpan PositionInChapter { get; set; }

    public DateTime LastListenedAt { get; set; } = DateTime.UtcNow;
}
