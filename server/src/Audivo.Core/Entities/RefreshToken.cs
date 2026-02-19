namespace Audivo.Core.Entities;

/// <summary>
/// Represents a rotation-based refresh token used to issue new JWT access tokens without re-authentication.
/// A token is considered active when it has not been revoked and has not expired.
/// </summary>
public class RefreshToken : BaseEntity
{
    public required string Token { get; set; }

    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive => !IsRevoked && !IsExpired;
}
