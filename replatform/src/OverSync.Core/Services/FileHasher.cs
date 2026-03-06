using System.Security.Cryptography;
using System.Text;

namespace OverSync.Core.Services;

public static class FileHasher
{
    public static string ComputeSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    public static async Task<string> ComputeFileSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var hasher = SHA256.Create();
        var hash = await hasher.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeStableStringHash(string value)
    {
        return ComputeSha256(Encoding.UTF8.GetBytes(value));
    }
}
