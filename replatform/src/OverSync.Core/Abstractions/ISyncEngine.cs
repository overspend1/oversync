using OverSync.Contracts;
using OverSync.Core.Models;

namespace OverSync.Core.Abstractions;

public interface ISyncEngine
{
    event EventHandler<SyncStatusDto>? SyncStatusChanged;
    event EventHandler<SyncProgressDto>? SyncProgressChanged;
    event EventHandler<ConflictDto>? ConflictDetected;
    event EventHandler<SyncErrorEventArgs>? SyncErrorOccurred;

    Task StartAsync(VaultConfig vaultConfig, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task ForceSyncAsync(CancellationToken cancellationToken = default);
    SyncStatusDto GetStatus();
}
