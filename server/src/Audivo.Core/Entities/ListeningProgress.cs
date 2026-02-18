namespace Audivo.Core.Entities;

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
