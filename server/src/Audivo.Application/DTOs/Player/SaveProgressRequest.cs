namespace Audivo.Application.DTOs.Player;

/// <summary>
/// Sent by the client periodically or on pause to persist the current playback position.
/// </summary>
public sealed record SaveProgressRequest(double PositionSeconds, double? TotalDurationSeconds = null);
