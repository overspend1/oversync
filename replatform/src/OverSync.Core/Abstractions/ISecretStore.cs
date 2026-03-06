namespace OverSync.Core.Abstractions;

public interface ISecretStore
{
    Task SaveVaultKey(Guid vaultId, byte[] key, CancellationToken cancellationToken = default);
    Task<byte[]?> LoadVaultKey(Guid vaultId, CancellationToken cancellationToken = default);
}
