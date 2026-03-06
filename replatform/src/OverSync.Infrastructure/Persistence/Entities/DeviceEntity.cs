namespace OverSync.Infrastructure.Persistence.Entities;

public sealed class DeviceEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid VaultId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime LastSeenUtc { get; set; }
}
