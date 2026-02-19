namespace Audivo.Application.DTOs.Player;

/// <summary>A user bookmark within an audiobook at a specific timestamp.</summary>
public sealed record BookmarkResponse(
    Guid Id,
    double PositionSeconds,
    string? Label,
    DateTime CreatedAt);
