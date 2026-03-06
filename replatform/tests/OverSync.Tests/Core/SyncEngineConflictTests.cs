using OverSync.Contracts;
using OverSync.Core.Abstractions;
using OverSync.Core.Models;
using OverSync.Core.Services;
using OverSync.Core.State;
using OverSync.Core.Sync;

namespace OverSync.Tests.Core;

public sealed class SyncEngineConflictTests
{
    [Fact]
    public async Task ForceSync_WhenRemoteWins_CreatesConflictCopy()
    {
        var vaultRoot = Path.Combine(Path.GetTempPath(), $"oversync-vault-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vaultRoot);
        var filePath = Path.Combine(vaultRoot, "note.md");
        await File.WriteAllTextAsync(filePath, "initial");

        var salt = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var passphrase = "passphrase";
        var dbPath = Path.Combine(Path.GetTempPath(), $"oversync-state-{Guid.NewGuid():N}.db");

        var crypto = new CryptoService();
        var api = new FakeSyncApiClient();
        var stateStore = new SqliteSyncStateStore();
        var engine = new SyncEngine(
            new NoOpWatcher(),
            api,
            stateStore,
            new NoOpScheduler(),
            crypto);

        var config = new VaultConfig(
            Guid.NewGuid(),
            vaultRoot,
            $"dev-{Guid.NewGuid():N}",
            "test-device",
            "windows",
            "http://localhost:5000",
            "token",
            "refresh",
            passphrase,
            salt,
            dbPath,
            TimeSpan.FromHours(1));

        await engine.StartAsync(config);

        await File.WriteAllTextAsync(filePath, "local-edited");
        File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);

        var remotePlain = System.Text.Encoding.UTF8.GetBytes("remote-wins");
        var vaultKey = await crypto.DeriveVaultKeyAsync(passphrase, salt);
        var encrypted = crypto.Encrypt(remotePlain, vaultKey);
        var envelope = new EncryptedFileEnvelope(
            "note.md",
            FileHasher.ComputeSha256(remotePlain),
            encrypted.Nonce,
            encrypted.Tag,
            encrypted.Ciphertext);
        var envelopeBytes = crypto.SerializeEnvelope(envelope);
        var chunks = FileChunker.Chunk(envelopeBytes);
        foreach (var chunk in chunks)
        {
            api.Chunks[chunk.Hash] = chunk.Content;
        }

        api.RemoteManifest = new ManifestDto(
            config.VaultId,
            999,
            DateTime.UtcNow,
            [
                new FileEntryDto(
                    "note.md",
                    FileHasher.ComputeSha256(remotePlain),
                    remotePlain.Length,
                    DateTime.UtcNow.AddMinutes(5),
                    999,
                    chunks.Select(c => new ChunkRefDto(c.Hash, c.Content.Length, c.Index)).ToList())
            ]);

        await engine.ForceSyncAsync();

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("remote-wins", content);

        var conflict = Directory.GetFiles(vaultRoot, "*.conflict-local-*").FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(conflict));
    }

    private sealed class NoOpWatcher : IFileWatcherAdapter
    {
        public void Start(string path, Action<string> callback)
        {
        }

        public void Stop()
        {
        }
    }

    private sealed class NoOpScheduler : IBackgroundScheduler
    {
        public Task StartAsync(TimeSpan interval, Func<CancellationToken, Task> callback, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;
    }

    private sealed class FakeSyncApiClient : ISyncApiClient
    {
        public ManifestDto? RemoteManifest { get; set; }

        public Dictionary<string, byte[]> Chunks { get; } = new(StringComparer.Ordinal);

        public Task<AuthTokenDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AuthTokenDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AuthTokenDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DeviceDto> RegisterDeviceAsync(DeviceRegistrationRequestDto request, string accessToken, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ManifestDto?> GetManifestAsync(Guid vaultId, string accessToken, CancellationToken cancellationToken = default)
            => Task.FromResult(RemoteManifest);

        public Task UploadManifestAsync(UploadManifestRequestDto request, string accessToken, CancellationToken cancellationToken = default)
        {
            RemoteManifest = request.Manifest;
            return Task.CompletedTask;
        }

        public async Task UploadChunkAsync(string hash, Stream content, string accessToken, CancellationToken cancellationToken = default)
        {
            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Chunks[hash] = buffer.ToArray();
        }

        public Task<byte[]> DownloadChunkAsync(string hash, string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Chunks[hash]);
        }

        public Task CommitAsync(CommitRequestDto request, string accessToken, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
