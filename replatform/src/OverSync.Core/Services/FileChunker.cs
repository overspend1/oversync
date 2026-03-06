using OverSync.Core.Models;

namespace OverSync.Core.Services;

public sealed class FileChunker
{
    public const int ChunkSizeBytes = 4 * 1024 * 1024;

    public static IReadOnlyList<FileChunk> Chunk(byte[] content)
    {
        if (content.Length == 0)
        {
            return [new FileChunk(0, [], FileHasher.ComputeSha256([]))];
        }

        var chunks = new List<FileChunk>();
        var index = 0;

        for (var offset = 0; offset < content.Length; offset += ChunkSizeBytes)
        {
            var size = Math.Min(ChunkSizeBytes, content.Length - offset);
            var chunk = new byte[size];
            Buffer.BlockCopy(content, offset, chunk, 0, size);
            chunks.Add(new FileChunk(index++, chunk, FileHasher.ComputeSha256(chunk)));
        }

        return chunks;
    }
}
