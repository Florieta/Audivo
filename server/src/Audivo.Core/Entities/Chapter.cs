namespace Audivo.Core.Entities;

/// <summary>
/// Represents a single audio chapter within an audiobook.
/// </summary>
public class Chapter : BaseEntity
{
    public required string Title { get; set; }

    public int OrderIndex { get; set; }

    public TimeSpan Duration { get; set; }

    public required string AudioFileUrl { get; set; }

    public Guid AudiobookId { get; set; }

    public Audiobook Audiobook { get; set; } = null!;
}
