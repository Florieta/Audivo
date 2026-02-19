namespace Audivo.Core.Entities;

/// <summary>
/// Represents an audiobook saved to a user's personal library.
/// </summary>
public class UserLibrary : BaseEntity
{
    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid AudiobookId { get; set; }

    public Audiobook Audiobook { get; set; } = null!;

    public DateTime AddedAt { get; init; } = DateTime.UtcNow;
}
