using CommunityToolkit.Mvvm.ComponentModel;
using OverSync.Contracts;
using OverSync.Core.Models;

namespace OverSync.Windows.Services;

public sealed partial class AppSessionState : ObservableObject
{
    [ObservableProperty]
    private bool _isOnboarded;

    [ObservableProperty]
    private bool _isSyncPaused;

    [ObservableProperty]
    private string _apiBaseUrl = "http://localhost:5000";

    [ObservableProperty]
    private VaultConfig? _vaultConfig;

    [ObservableProperty]
    private AuthTokenDto? _tokens;

    public IList<string> Logs { get; } = new List<string>();

    public void Log(string message)
    {
        Logs.Add($"[{DateTime.UtcNow:O}] {message}");
    }
}
