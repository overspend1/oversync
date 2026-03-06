namespace OverSync.Core.Models;

public sealed record FileChunk(
    int Index,
    byte[] Content,
    string Hash);
