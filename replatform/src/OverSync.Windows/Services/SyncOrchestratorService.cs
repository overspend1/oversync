using OverSync.Contracts;
using OverSync.Core.Abstractions;
using OverSync.Core.Models;

namespace OverSync.Windows.Services;

public sealed class SyncOrchestratorService
{
    private readonly ISyncEngine _syncEngine;
    private readonly AppSessionState _sessionState;

    public SyncOrchestratorService(ISyncEngine syncEngine, AppSessionState sessionState)
    {
        _syncEngine = syncEngine;
        _sessionState = sessionState;

        _syncEngine.SyncStatusChanged += (_, status) =>
        {
            _sessionState.Log($"Status update: running={status.IsRunning}, syncing={status.IsSyncing}, queue={status.QueueLength}");
        };

        _syncEngine.SyncProgressChanged += (_, progress) =>
        {
            _sessionState.Log($"Progress: {progress.ProcessedFiles}/{progress.TotalFiles}, uploaded={progress.UploadedBytes}");
        };

        _syncEngine.ConflictDetected += (_, conflict) =>
        {
            _sessionState.Log($"Conflict detected: {conflict.Path} ({conflict.Resolution})");
        };

        _syncEngine.SyncErrorOccurred += (_, error) =>
        {
            _sessionState.Log($"Sync error: {error.Message} - {error.Exception.Message}");
        };
    }

    public async Task StartAsync(VaultConfig config, CancellationToken cancellationToken = default)
    {
        _sessionState.VaultConfig = config;
        _sessionState.IsOnboarded = true;
        _sessionState.IsSyncPaused = false;
        await _syncEngine.StartAsync(config, cancellationToken);
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionState.IsSyncPaused)
        {
            return;
        }

        _sessionState.IsSyncPaused = true;
        await _syncEngine.StopAsync(cancellationToken);
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionState.IsSyncPaused || _sessionState.VaultConfig is null)
        {
            return;
        }

        _sessionState.IsSyncPaused = false;
        await _syncEngine.StartAsync(_sessionState.VaultConfig, cancellationToken);
    }

    public SyncStatusDto CurrentStatus() => _syncEngine.GetStatus();

    public Task ForceSyncAsync(CancellationToken cancellationToken = default) => _syncEngine.ForceSyncAsync(cancellationToken);
}
