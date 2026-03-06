using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OverSync.Windows.Services;

namespace OverSync.Windows.ViewModels;

public sealed partial class MainShellViewModel : ObservableObject
{
    private readonly SyncOrchestratorService _orchestrator;
    private readonly AppSessionState _session;
    private readonly IWindowControlService _windowControl;

    [ObservableProperty]
    private bool _isOnboarded;

    [ObservableProperty]
    private string _pauseResumeText = "Pause Sync";

    public MainShellViewModel(
        SyncOrchestratorService orchestrator,
        AppSessionState session,
        IWindowControlService windowControl)
    {
        _orchestrator = orchestrator;
        _session = session;
        _windowControl = windowControl;

        IsOnboarded = _session.IsOnboarded;
        PauseResumeText = _session.IsSyncPaused ? "Resume Sync" : "Pause Sync";

        _session.PropertyChanged += (_, _) =>
        {
            IsOnboarded = _session.IsOnboarded;
            PauseResumeText = _session.IsSyncPaused ? "Resume Sync" : "Pause Sync";
        };
    }

    [RelayCommand]
    private async Task PauseOrResumeAsync()
    {
        if (_session.IsSyncPaused)
        {
            await _orchestrator.ResumeAsync();
            return;
        }

        await _orchestrator.PauseAsync();
    }

    [RelayCommand]
    private Task ForceSyncAsync() => _orchestrator.ForceSyncAsync();

    [RelayCommand]
    private void MinimizeToTray() => _windowControl.MinimizeToBackground();

    [RelayCommand]
    private void Quit() => _windowControl.Quit();
}
