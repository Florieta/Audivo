namespace Audivo.Core.Entities;

public class Audiobook : BaseEntity
{
    public required string Title { get; set; }

    public required string Author { get; set; }

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? Narrator { get; set; }

    public TimeSpan TotalDuration { get; set; }

    public ICollection<Chapter> Chapters { get; set; } = [];
}
