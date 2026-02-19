namespace Audivo.Core.Entities;

public class Bookmark : BaseEntity
{
    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid AudiobookId { get; set; }

    public Audiobook Audiobook { get; set; } = null!;

    public string? Label { get; set; }

    public double PositionSeconds { get; set; }
}
