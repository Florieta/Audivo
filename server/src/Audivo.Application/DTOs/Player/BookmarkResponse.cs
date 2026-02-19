namespace Audivo.Application.DTOs.Player;

public sealed record BookmarkResponse(
    Guid Id,
    double PositionSeconds,
    string? Label,
    DateTime CreatedAt);
