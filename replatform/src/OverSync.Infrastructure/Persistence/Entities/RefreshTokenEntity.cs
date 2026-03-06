namespace OverSync.Infrastructure.Persistence.Entities;

public sealed class RefreshTokenEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
}
