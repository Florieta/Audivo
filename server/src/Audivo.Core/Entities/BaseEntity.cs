namespace Audivo.Core.Entities;

/// <summary>
/// Base entity providing common audit fields for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets the unique identifier for this entity.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the UTC timestamp when this entity was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp of the last update, or <c>null</c> if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
