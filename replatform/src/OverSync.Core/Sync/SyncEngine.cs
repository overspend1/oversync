using System.Collections.Concurrent;
using OverSync.Contracts;
using OverSync.Core.Abstractions;
using OverSync.Core.Models;
using OverSync.Core.Services;

namespace OverSync.Core.Sync;

public sealed class SyncEngine : ISyncEngine
{
    private readonly IFileWatcherAdapter _watcher;
    private readonly ISyncApiClient _apiClient;
    private readonly ISyncStateStore _stateStore;
    private readonly IBackgroundScheduler _scheduler;
    private readonly CryptoService _cryptoService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly ConcurrentDictionary<string, byte> _inMemoryQueue = new(StringComparer.OrdinalIgnoreCase);

    private VaultConfig? _config;
    private byte[]? _vaultKey;
    private bool _isStarted;
    private SyncStatusDto _status = new(false, false, null, 0, 0, 0);

    public SyncEngine(
        IFileWatcherAdapter watcher,
        ISyncApiClient apiClient,
        ISyncStateStore stateStore,
        IBackgroundScheduler scheduler,
        CryptoService cryptoService)
    {
        _watcher = watcher;
        _apiClient = apiClient;
        _stateStore = stateStore;
        _scheduler = scheduler;
        _cryptoService = cryptoService;
    }

    public event EventHandler<SyncStatusDto>? SyncStatusChanged;
    public event EventHandler<SyncProgressDto>? SyncProgressChanged;
    public event EventHandler<ConflictDto>? ConflictDetected;
    public event EventHandler<SyncErrorEventArgs>? SyncErrorOccurred;

    public async Task StartAsync(VaultConfig vaultConfig, CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            return;
        }

        if (!Directory.Exists(vaultConfig.VaultPath))
        {
            throw new DirectoryNotFoundException($"Vault path does not exist: {vaultConfig.VaultPath}");
        }

        _config = vaultConfig;
        _vaultKey = await _cryptoService.DeriveVaultKeyAsync(
            vaultConfig.Passphrase,
            vaultConfig.VaultSalt,
            cancellationToken: cancellationToken);

        await _stateStore.InitializeAsync(vaultConfig.StateDatabasePath, cancellationToken);
        await SeedInitialQueueAsync(vaultConfig, cancellationToken);

        _watcher.Start(vaultConfig.VaultPath, changedPath =>
        {
            if (_config is null)
            {
                return;
            }

            var relative = FileDiscovery.ToRelativePath(_config.VaultPath, changedPath);
            _ = Task.Run(async () =>
            {
                _inMemoryQueue[relative] = 1;
                await _stateStore.QueuePendingChangeAsync(_config.VaultId, relative);
                await RefreshQueueCountAsync(_config.VaultId);
            });
        });

        await _scheduler.StartAsync(
            vaultConfig.SyncInterval,
            ct => ForceSyncAsync(ct),
            cancellationToken);

        _isStarted = true;
        await RefreshStatusAsync(_status with { IsRunning = true }, cancellationToken);

        await ForceSyncAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            return;
        }

        await _scheduler.StopAsync();
        _watcher.Stop();
        _isStarted = false;
        await RefreshStatusAsync(_status with { IsRunning = false, IsSyncing = false }, cancellationToken);
    }

    public async Task ForceSyncAsync(CancellationToken cancellationToken = default)
    {
        if (_config is null || _vaultKey is null || !_isStarted)
        {
            return;
        }

        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            await RefreshStatusAsync(_status with { IsSyncing = true }, cancellationToken);

            var remoteManifest = await RetryPolicy.ExecuteAsync(
                () => _apiClient.GetManifestAsync(_config.VaultId, _config.AccessToken, cancellationToken),
                cancellationToken: cancellationToken);

            if (remoteManifest is not null)
            {
                await ApplyRemoteManifestAsync(remoteManifest, cancellationToken);
            }

            var pending = await _stateStore.DequeuePendingChangesAsync(_config.VaultId, 2048, cancellationToken);
            foreach (var path in pending)
            {
                _inMemoryQueue.TryRemove(path, out _);
            }

            if (pending.Count == 0)
            {
                await RefreshQueueCountAsync(_config.VaultId, cancellationToken);
                await RefreshStatusAsync(_status with
                {
                    IsSyncing = false,
                    LastSyncUtc = DateTime.UtcNow
                }, cancellationToken);
                return;
            }

            var trackedFiles = (await _stateStore.GetTrackedFilesAsync(_config.VaultId, cancellationToken))
                .ToDictionary(static item => item.Key, static item => item.Value, StringComparer.OrdinalIgnoreCase);

            var cursor = await _stateStore.GetSyncCursorAsync(_config.VaultId, cancellationToken) ?? 0;
            var remoteVersion = remoteManifest?.Version ?? 0;
            var nextVersion = Math.Max(cursor, remoteVersion) + 1;
            var progress = new SyncProgressDto(0, pending.Count, 0, 0);

            foreach (var relative in pending)
            {
                var fullPath = Path.Combine(_config.VaultPath, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    trackedFiles.Remove(relative);
                    progress = progress with { ProcessedFiles = progress.ProcessedFiles + 1 };
                    SyncProgressChanged?.Invoke(this, progress);
                    continue;
                }

                var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                var hash = FileHasher.ComputeSha256(bytes);
                var encrypted = _cryptoService.Encrypt(bytes, _vaultKey);
                var envelope = new EncryptedFileEnvelope(relative, hash, encrypted.Nonce, encrypted.Tag, encrypted.Ciphertext);
                var envelopeBytes = _cryptoService.SerializeEnvelope(envelope);
                var chunks = FileChunker.Chunk(envelopeBytes);

                foreach (var chunk in chunks)
                {
                    var chunkBuffer = chunk.Content;
                    await RetryPolicy.ExecuteAsync(async () =>
                    {
                        await using var stream = new MemoryStream(chunkBuffer, writable: false);
                        await _apiClient.UploadChunkAsync(chunk.Hash, stream, _config.AccessToken, cancellationToken);
                    }, cancellationToken: cancellationToken);

                    progress = progress with { UploadedBytes = progress.UploadedBytes + chunkBuffer.LongLength };
                }

                var fileEntry = new TrackedFileState(
                    relative,
                    hash,
                    nextVersion,
                    File.GetLastWriteTimeUtc(fullPath),
                    chunks.Select(c => new ChunkRefDto(c.Hash, c.Content.LongLength, c.Index)).ToList());

                trackedFiles[relative] = fileEntry;
                await _stateStore.UpsertTrackedFileAsync(_config.VaultId, fileEntry, cancellationToken);

                progress = progress with { ProcessedFiles = progress.ProcessedFiles + 1 };
                SyncProgressChanged?.Invoke(this, progress);
            }

            var manifest = new ManifestDto(
                _config.VaultId,
                nextVersion,
                DateTime.UtcNow,
                trackedFiles.Values
                    .Select(f => new FileEntryDto(f.Path, f.Hash, f.Chunks.Sum(c => c.Size), f.LastModifiedUtc, f.Version, f.Chunks))
                    .OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                    .ToList());

            await RetryPolicy.ExecuteAsync(
                () => _apiClient.UploadManifestAsync(new UploadManifestRequestDto(manifest), _config.AccessToken, cancellationToken),
                cancellationToken: cancellationToken);
            await RetryPolicy.ExecuteAsync(
                () => _apiClient.CommitAsync(new CommitRequestDto(_config.VaultId, _config.DeviceId, manifest.Version), _config.AccessToken, cancellationToken),
                cancellationToken: cancellationToken);

            await _stateStore.SetSyncCursorAsync(_config.VaultId, manifest.Version, cancellationToken);
            await RefreshQueueCountAsync(_config.VaultId, cancellationToken);
            await RefreshStatusAsync(_status with
            {
                IsSyncing = false,
                LastSyncUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            SyncErrorOccurred?.Invoke(this, new SyncErrorEventArgs("Sync run failed.", ex));
            await RefreshStatusAsync(_status with { IsSyncing = false }, cancellationToken);
            throw;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public SyncStatusDto GetStatus() => _status;

    private async Task SeedInitialQueueAsync(VaultConfig config, CancellationToken cancellationToken)
    {
        foreach (var file in FileDiscovery.EnumerateFiles(config.VaultPath))
        {
            var relative = FileDiscovery.ToRelativePath(config.VaultPath, file);
            _inMemoryQueue[relative] = 1;
            await _stateStore.QueuePendingChangeAsync(config.VaultId, relative, cancellationToken);
        }

        await RefreshQueueCountAsync(config.VaultId, cancellationToken);
    }

    private async Task ApplyRemoteManifestAsync(ManifestDto remoteManifest, CancellationToken cancellationToken)
    {
        if (_config is null || _vaultKey is null)
        {
            return;
        }

        var tracked = await _stateStore.GetTrackedFilesAsync(_config.VaultId, cancellationToken);
        foreach (var remoteFile in remoteManifest.Files)
        {
            var fullPath = Path.Combine(_config.VaultPath, remoteFile.Path.Replace('/', Path.DirectorySeparatorChar));
            var localTracked = tracked.TryGetValue(remoteFile.Path, out var trackedState) ? trackedState : null;
            var localExists = File.Exists(fullPath);
            var localLastModified = localExists ? File.GetLastWriteTimeUtc(fullPath) : DateTime.MinValue;

            var localChanged =
                localExists &&
                localTracked is not null &&
                localLastModified > localTracked.LastModifiedUtc &&
                !string.Equals(localTracked.Hash, await FileHasher.ComputeFileSha256Async(fullPath, cancellationToken), StringComparison.Ordinal);

            var remoteChanged = localTracked is null || !string.Equals(localTracked.Hash, remoteFile.Hash, StringComparison.Ordinal);

            if (!remoteChanged)
            {
                continue;
            }

            if (localChanged)
            {
                var winner = remoteFile.LastModifiedUtc >= localLastModified ? "remote" : "local";
                if (winner == "remote" && localExists)
                {
                    await CreateConflictCopyAsync(fullPath, cancellationToken);
                }
                else
                {
                    var remoteBytes = await DownloadAndDecryptFileAsync(remoteFile, cancellationToken);
                    await WriteConflictCopyForRemoteAsync(fullPath, remoteBytes, cancellationToken);
                    continue;
                }

                var conflict = new ConflictDto(
                    remoteFile.Path,
                    localTracked?.Hash ?? "missing",
                    remoteFile.Hash,
                    $"last-write-wins:{winner}",
                    DateTime.UtcNow);
                await _stateStore.SaveConflictAsync(_config.VaultId, conflict, cancellationToken);
                ConflictDetected?.Invoke(this, conflict);
            }

            var plaintext = await DownloadAndDecryptFileAsync(remoteFile, cancellationToken);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(fullPath, plaintext, cancellationToken);
            File.SetLastWriteTimeUtc(fullPath, remoteFile.LastModifiedUtc);

            var trackedFile = new TrackedFileState(
                remoteFile.Path,
                remoteFile.Hash,
                remoteFile.Version,
                remoteFile.LastModifiedUtc,
                remoteFile.Chunks);
            await _stateStore.UpsertTrackedFileAsync(_config.VaultId, trackedFile, cancellationToken);
        }

        await _stateStore.SetSyncCursorAsync(_config.VaultId, remoteManifest.Version, cancellationToken);
        var conflictCount = await _stateStore.GetConflictCountAsync(_config.VaultId, cancellationToken);
        await RefreshStatusAsync(_status with { ConflictCount = conflictCount }, cancellationToken);
    }

    private async Task<byte[]> DownloadAndDecryptFileAsync(FileEntryDto remoteFile, CancellationToken cancellationToken)
    {
        if (_config is null || _vaultKey is null)
        {
            return [];
        }

        await using var envelopeStream = new MemoryStream();
        foreach (var chunk in remoteFile.Chunks.OrderBy(c => c.Index))
        {
            var data = await RetryPolicy.ExecuteAsync(
                () => _apiClient.DownloadChunkAsync(chunk.Hash, _config.AccessToken, cancellationToken),
                cancellationToken: cancellationToken);
            await envelopeStream.WriteAsync(data, cancellationToken);
        }

        var envelope = _cryptoService.DeserializeEnvelope(envelopeStream.ToArray());
        return _cryptoService.Decrypt(
            new EncryptedPayload(envelope.Ciphertext, envelope.Nonce, envelope.Tag),
            _vaultKey);
    }

    private static async Task CreateConflictCopyAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var extension = Path.GetExtension(sourcePath);
        var baseName = sourcePath[..^extension.Length];
        var copyPath = $"{baseName}.conflict-local-{stamp}{extension}";
        await using var source = File.OpenRead(sourcePath);
        await using var target = File.Create(copyPath);
        await source.CopyToAsync(target, cancellationToken);
    }

    private static async Task WriteConflictCopyForRemoteAsync(string targetPath, byte[] remoteBytes, CancellationToken cancellationToken)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var extension = Path.GetExtension(targetPath);
        var baseName = targetPath[..^extension.Length];
        var copyPath = $"{baseName}.conflict-remote-{stamp}{extension}";
        await File.WriteAllBytesAsync(copyPath, remoteBytes, cancellationToken);
    }

    private async Task RefreshQueueCountAsync(Guid vaultId, CancellationToken cancellationToken = default)
    {
        var pending = await _stateStore.GetPendingCountAsync(vaultId, cancellationToken);
        var conflicts = await _stateStore.GetConflictCountAsync(vaultId, cancellationToken);
        await RefreshStatusAsync(_status with { QueueLength = pending, ConflictCount = conflicts }, cancellationToken);
    }

    private Task RefreshStatusAsync(SyncStatusDto newStatus, CancellationToken cancellationToken = default)
    {
        _status = newStatus;
        SyncStatusChanged?.Invoke(this, _status);
        return Task.CompletedTask;
    }
}
