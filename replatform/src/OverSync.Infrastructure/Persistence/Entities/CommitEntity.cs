namespace OverSync.Infrastructure.Persistence.Entities;

public sealed class CommitEntity
{
    public Guid CommitId { get; set; }
    public Guid UserId { get; set; }
    public Guid VaultId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
