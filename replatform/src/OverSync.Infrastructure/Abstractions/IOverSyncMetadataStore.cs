using OverSync.Contracts;

namespace OverSync.Infrastructure.Abstractions;

public interface IOverSyncMetadataStore
{
    Task<UserRecord?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserRecord> CreateUserAsync(string email, string passwordHash, CancellationToken cancellationToken = default);

    Task<RefreshTokenRecord> CreateRefreshTokenAsync(
        Guid userId,
        string token,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenRecord?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RotateRefreshTokenAsync(
        string oldToken,
        string newToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<DeviceDto> RegisterDeviceAsync(
        Guid userId,
        string deviceId,
        DeviceRegistrationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ManifestDto?> GetManifestAsync(Guid vaultId, CancellationToken cancellationToken = default);
    Task SaveManifestAsync(Guid userId, ManifestDto manifest, CancellationToken cancellationToken = default);
    Task SaveCommitAsync(Guid userId, Guid vaultId, string deviceId, long version, CancellationToken cancellationToken = default);
}

public sealed record UserRecord(Guid UserId, string Email, string PasswordHash, DateTime CreatedAtUtc);

public sealed record RefreshTokenRecord(Guid UserId, string Token, DateTime ExpiresAtUtc, bool IsRevoked);
