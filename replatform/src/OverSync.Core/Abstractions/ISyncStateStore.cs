using OverSync.Contracts;
using OverSync.Core.Models;

namespace OverSync.Core.Abstractions;

public interface ISyncStateStore
{
    Task InitializeAsync(string databasePath, CancellationToken cancellationToken = default);
    Task<long?> GetSyncCursorAsync(Guid vaultId, CancellationToken cancellationToken = default);
    Task SetSyncCursorAsync(Guid vaultId, long version, CancellationToken cancellationToken = default);
    Task QueuePendingChangeAsync(Guid vaultId, string relativePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> DequeuePendingChangesAsync(Guid vaultId, int maxItems, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(Guid vaultId, CancellationToken cancellationToken = default);
    Task UpsertTrackedFileAsync(Guid vaultId, TrackedFileState file, CancellationToken cancellationToken = default);
    Task<TrackedFileState?> GetTrackedFileAsync(Guid vaultId, string relativePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, TrackedFileState>> GetTrackedFilesAsync(Guid vaultId, CancellationToken cancellationToken = default);
    Task SaveConflictAsync(Guid vaultId, ConflictDto conflict, CancellationToken cancellationToken = default);
    Task<int> GetConflictCountAsync(Guid vaultId, CancellationToken cancellationToken = default);
}
