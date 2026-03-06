namespace OverSync.Core.Models;

public sealed record VaultConfig(
    Guid VaultId,
    string VaultPath,
    string DeviceId,
    string DeviceName,
    string Platform,
    string ApiBaseUrl,
    string AccessToken,
    string RefreshToken,
    string Passphrase,
    byte[] VaultSalt,
    string StateDatabasePath,
    TimeSpan SyncInterval);
