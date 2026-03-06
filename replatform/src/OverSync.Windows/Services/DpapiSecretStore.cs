using System.Security.Cryptography;
using OverSync.Core.Abstractions;

namespace OverSync.Windows.Services;

public sealed class DpapiSecretStore : ISecretStore
{
    private readonly string _secretsPath;

    public DpapiSecretStore()
    {
        _secretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OverSync",
            "Secrets");
        Directory.CreateDirectory(_secretsPath);
    }

    public async Task SaveVaultKey(Guid vaultId, byte[] key, CancellationToken cancellationToken = default)
    {
        var protectedData = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        var path = Path.Combine(_secretsPath, $"{vaultId:D}.bin");
        await File.WriteAllBytesAsync(path, protectedData, cancellationToken);
    }

    public async Task<byte[]?> LoadVaultKey(Guid vaultId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_secretsPath, $"{vaultId:D}.bin");
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedData = await File.ReadAllBytesAsync(path, cancellationToken);
        return ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
    }
}
