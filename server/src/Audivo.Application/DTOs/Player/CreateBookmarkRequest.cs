namespace Audivo.Application.DTOs.Player;

/// <summary>
/// Sent by the client to create a bookmark at a specific timestamp.
/// </summary>
public sealed record CreateBookmarkRequest(double PositionSeconds, string? Label);
