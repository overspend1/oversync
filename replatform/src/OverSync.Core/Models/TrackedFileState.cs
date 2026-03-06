using OverSync.Contracts;

namespace OverSync.Core.Models;

public sealed record TrackedFileState(
    string Path,
    string Hash,
    long Version,
    DateTime LastModifiedUtc,
    IReadOnlyList<ChunkRefDto> Chunks);
