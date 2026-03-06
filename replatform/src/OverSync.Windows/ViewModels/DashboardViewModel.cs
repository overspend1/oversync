using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OverSync.Windows.Services;

namespace OverSync.Windows.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly SyncOrchestratorService _orchestrator;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private DateTime? _lastSyncUtc;

    [ObservableProperty]
    private int _queueLength;

    [ObservableProperty]
    private int _conflictCount;

    [ObservableProperty]
    private int _connectedDevices;

    public DashboardViewModel(SyncOrchestratorService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await Task.Yield();
        var status = _orchestrator.CurrentStatus();
        IsRunning = status.IsRunning;
        IsSyncing = status.IsSyncing;
        LastSyncUtc = status.LastSyncUtc;
        QueueLength = status.QueueLength;
        ConflictCount = status.ConflictCount;
        ConnectedDevices = status.ConnectedDevices;
    }
}
