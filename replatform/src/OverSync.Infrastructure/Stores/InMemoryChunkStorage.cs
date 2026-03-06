using System.Collections.Concurrent;
using OverSync.Infrastructure.Abstractions;

namespace OverSync.Infrastructure.Stores;

public sealed class InMemoryChunkStorage : IChunkStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _chunks = new(StringComparer.Ordinal);

    public async Task StoreAsync(string hash, Stream content, CancellationToken cancellationToken = default)
    {
        await using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        _chunks[hash] = copy.ToArray();
    }

    public Task<byte[]?> ReadAsync(string hash, CancellationToken cancellationToken = default)
    {
        _chunks.TryGetValue(hash, out var chunk);
        return Task.FromResult(chunk);
    }
}
