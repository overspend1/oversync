namespace OverSync.Infrastructure.Abstractions;

public interface IChunkStorage
{
    Task StoreAsync(string hash, Stream content, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadAsync(string hash, CancellationToken cancellationToken = default);
}
