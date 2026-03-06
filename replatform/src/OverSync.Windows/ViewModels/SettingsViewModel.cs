using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OverSync.Windows.Services;

namespace OverSync.Windows.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSessionState _sessionState;

    [ObservableProperty]
    private string _apiBaseUrl;

    [ObservableProperty]
    private int _syncIntervalSeconds = 30;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(AppSessionState sessionState)
    {
        _sessionState = sessionState;
        _apiBaseUrl = sessionState.ApiBaseUrl;

        if (sessionState.VaultConfig is not null)
        {
            SyncIntervalSeconds = Math.Max(5, (int)sessionState.VaultConfig.SyncInterval.TotalSeconds);
        }
    }

    [RelayCommand]
    private void Save()
    {
        _sessionState.ApiBaseUrl = ApiBaseUrl.Trim();
        StatusMessage = "Settings saved. Restart app to apply API endpoint changes.";
        _sessionState.Log($"Settings updated: api={_sessionState.ApiBaseUrl}, syncInterval={SyncIntervalSeconds}s");
    }
}
