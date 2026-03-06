using OverSync.Infrastructure.Abstractions;

namespace OverSync.Infrastructure.Stores;

public sealed class LocalFileChunkStorage : IChunkStorage
{
    private readonly string _root;

    public LocalFileChunkStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task StoreAsync(string hash, Stream content, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, $"{hash}.bin");
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string hash, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, $"{hash}.bin");
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }
}
