using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OverSync.Contracts;
using OverSync.Infrastructure.Abstractions;
using OverSync.Infrastructure.Persistence;
using OverSync.Infrastructure.Persistence.Entities;

namespace OverSync.Infrastructure.Stores;

public sealed class EfCoreMetadataStore : IOverSyncMetadataStore
{
    private readonly InfrastructureDbContext _dbContext;

    public EfCoreMetadataStore(InfrastructureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserRecord?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        return user is null
            ? null
            : new UserRecord(user.UserId, user.Email, user.PasswordHash, user.CreatedAtUtc);
    }

    public async Task<UserRecord> CreateUserAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
    {
        var entity = new UserEntity
        {
            UserId = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserRecord(entity.UserId, entity.Email, entity.PasswordHash, entity.CreatedAtUtc);
    }

    public async Task<RefreshTokenRecord> CreateRefreshTokenAsync(
        Guid userId,
        string token,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = new RefreshTokenEntity
        {
            Token = token,
            UserId = userId,
            ExpiresAtUtc = expiresAtUtc,
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new RefreshTokenRecord(entity.UserId, entity.Token, entity.ExpiresAtUtc, entity.IsRevoked);
    }

    public async Task<RefreshTokenRecord?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Token == token, cancellationToken);

        return entity is null
            ? null
            : new RefreshTokenRecord(entity.UserId, entity.Token, entity.ExpiresAtUtc, entity.IsRevoked);
    }

    public async Task RotateRefreshTokenAsync(
        string oldToken,
        string newToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var current = await _dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == oldToken, cancellationToken);
        if (current is null)
        {
            return;
        }

        current.IsRevoked = true;
        _dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Token = newToken,
            UserId = current.UserId,
            ExpiresAtUtc = expiresAtUtc,
            IsRevoked = false
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeviceDto> RegisterDeviceAsync(
        Guid userId,
        string deviceId,
        DeviceRegistrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Devices.SingleOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
        if (entity is null)
        {
            entity = new DeviceEntity
            {
                DeviceId = deviceId,
                UserId = userId,
                VaultId = request.VaultId,
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                LastSeenUtc = DateTime.UtcNow
            };
            _dbContext.Devices.Add(entity);
        }
        else
        {
            entity.LastSeenUtc = DateTime.UtcNow;
            entity.DeviceName = request.DeviceName;
            entity.Platform = request.Platform;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeviceDto(entity.DeviceId, entity.VaultId, entity.DeviceName, entity.Platform, entity.LastSeenUtc);
    }

    public async Task<ManifestDto?> GetManifestAsync(Guid vaultId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Manifests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.VaultId == vaultId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ManifestDto>(entity.JsonPayload);
    }

    public async Task SaveManifestAsync(Guid userId, ManifestDto manifest, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Manifests.SingleOrDefaultAsync(x => x.VaultId == manifest.VaultId, cancellationToken);
        var payload = JsonSerializer.Serialize(manifest);

        if (entity is null)
        {
            entity = new ManifestEntity
            {
                VaultId = manifest.VaultId,
                UserId = userId,
                Version = manifest.Version,
                GeneratedAtUtc = manifest.GeneratedAtUtc,
                JsonPayload = payload
            };
            _dbContext.Manifests.Add(entity);
        }
        else
        {
            entity.Version = manifest.Version;
            entity.GeneratedAtUtc = manifest.GeneratedAtUtc;
            entity.JsonPayload = payload;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCommitAsync(
        Guid userId,
        Guid vaultId,
        string deviceId,
        long version,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Commits.Add(new CommitEntity
        {
            CommitId = Guid.NewGuid(),
            UserId = userId,
            VaultId = vaultId,
            DeviceId = deviceId,
            Version = version,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
