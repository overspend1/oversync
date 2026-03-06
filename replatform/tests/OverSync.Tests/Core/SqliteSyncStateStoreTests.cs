using OverSync.Contracts;
using OverSync.Core.Models;
using OverSync.Core.State;

namespace OverSync.Tests.Core;

public sealed class SqliteSyncStateStoreTests
{
    [Fact]
    public async Task DequeuePendingChanges_PreservesOrder()
    {
        var store = new SqliteSyncStateStore();
        var vaultId = Guid.NewGuid();
        var dbPath = Path.Combine(Path.GetTempPath(), $"oversync-state-{Guid.NewGuid():N}.db");

        await store.InitializeAsync(dbPath);
        await store.QueuePendingChangeAsync(vaultId, "b.md");
        await store.QueuePendingChangeAsync(vaultId, "a.md");
        await store.QueuePendingChangeAsync(vaultId, "c.md");

        var dequeued = await store.DequeuePendingChangesAsync(vaultId, 10);
        Assert.Equal(new[] { "b.md", "a.md", "c.md" }, dequeued);
    }

    [Fact]
    public async Task UpsertTrackedFile_RoundTripsChunkRefs()
    {
        var store = new SqliteSyncStateStore();
        var vaultId = Guid.NewGuid();
        var dbPath = Path.Combine(Path.GetTempPath(), $"oversync-state-{Guid.NewGuid():N}.db");

        await store.InitializeAsync(dbPath);
        var tracked = new TrackedFileState(
            "note.md",
            "hash",
            1,
            DateTime.UtcNow,
            [new ChunkRefDto("chunk-a", 100, 0), new ChunkRefDto("chunk-b", 20, 1)]);

        await store.UpsertTrackedFileAsync(vaultId, tracked);
        var loaded = await store.GetTrackedFileAsync(vaultId, "note.md");

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Chunks.Count);
        Assert.Equal("chunk-a", loaded.Chunks[0].Hash);
    }
}
