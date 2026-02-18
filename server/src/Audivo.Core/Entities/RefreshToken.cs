namespace Audivo.Core.Entities;

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
