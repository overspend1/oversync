namespace OverSync.Infrastructure.Persistence.Entities;

public sealed class ManifestEntity
{
    public Guid VaultId { get; set; }
    public Guid UserId { get; set; }
    public long Version { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string JsonPayload { get; set; } = string.Empty;
}
