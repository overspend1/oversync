using System.Collections.Concurrent;
using OverSync.Contracts;
using OverSync.Infrastructure.Abstractions;

namespace OverSync.Infrastructure.Stores;

public sealed class InMemoryMetadataStore : IOverSyncMetadataStore
{
    private readonly ConcurrentDictionary<string, UserRecord> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _tokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, ManifestDto> _manifests = new();
    private readonly ConcurrentDictionary<string, DeviceDto> _devices = new(StringComparer.OrdinalIgnoreCase);

    public Task<UserRecord?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task<UserRecord> CreateUserAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (_usersByEmail.ContainsKey(email))
        {
            throw new InvalidOperationException("User already exists.");
        }

        var user = new UserRecord(Guid.NewGuid(), email, passwordHash, DateTime.UtcNow);
        if (!_usersByEmail.TryAdd(email, user))
        {
            throw new InvalidOperationException("Could not persist user.");
        }

        return Task.FromResult(user);
    }

    public Task<RefreshTokenRecord> CreateRefreshTokenAsync(Guid userId, string token, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var refresh = new RefreshTokenRecord(userId, token, expiresAtUtc, false);
        _tokens[token] = refresh;
        return Task.FromResult(refresh);
    }

    public Task<RefreshTokenRecord?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task RotateRefreshTokenAsync(string oldToken, string newToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(oldToken, out var existing))
        {
            _tokens[oldToken] = existing with { IsRevoked = true };
            _tokens[newToken] = new RefreshTokenRecord(existing.UserId, newToken, expiresAtUtc, false);
        }

        return Task.CompletedTask;
    }

    public Task<DeviceDto> RegisterDeviceAsync(
        Guid userId,
        string deviceId,
        DeviceRegistrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var device = new DeviceDto(
            deviceId,
            request.VaultId,
            request.DeviceName,
            request.Platform,
            DateTime.UtcNow);

        _devices[deviceId] = device;
        return Task.FromResult(device);
    }

    public Task<ManifestDto?> GetManifestAsync(Guid vaultId, CancellationToken cancellationToken = default)
    {
        _manifests.TryGetValue(vaultId, out var manifest);
        return Task.FromResult(manifest);
    }

    public Task SaveManifestAsync(Guid userId, ManifestDto manifest, CancellationToken cancellationToken = default)
    {
        _manifests[manifest.VaultId] = manifest;
        return Task.CompletedTask;
    }

    public Task SaveCommitAsync(Guid userId, Guid vaultId, string deviceId, long version, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
