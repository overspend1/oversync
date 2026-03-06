using Microsoft.UI.Dispatching;
using OverSync.Windows.ViewModels;

namespace OverSync.Windows.Views;

public sealed class DashboardPage : Page
{
    private readonly TextBlock _running = new();
    private readonly TextBlock _syncing = new();
    private readonly TextBlock _lastSync = new();
    private readonly TextBlock _queue = new();
    private readonly TextBlock _conflicts = new();
    private readonly TextBlock _devices = new();
    private readonly DispatcherQueueTimer _timer;

    private DashboardViewModel? _viewModel;

    public DashboardPage()
    {
        var layout = new StackPanel { Margin = new Thickness(24), Spacing = 10 };
        layout.Children.Add(new TextBlock { Text = "Dashboard", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var refreshButton = new Button { Content = "Refresh", HorizontalAlignment = HorizontalAlignment.Left };
        refreshButton.Click += async (_, _) => await RefreshAsync();
        layout.Children.Add(refreshButton);
        layout.Children.Add(_running);
        layout.Children.Add(_syncing);
        layout.Children.Add(_lastSync);
        layout.Children.Add(_queue);
        layout.Children.Add(_conflicts);
        layout.Children.Add(_devices);
        Content = layout;

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += async (_, _) => await RefreshAsync();

        Loaded += async (_, _) =>
        {
            _viewModel ??= App.Services.GetRequiredService<DashboardViewModel>();
            await RefreshAsync();
            _timer.Start();
        };
    }

    private async Task RefreshAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.RefreshCommand.ExecuteAsync(null);
        _running.Text = $"Running: {_viewModel.IsRunning}";
        _syncing.Text = $"Syncing: {_viewModel.IsSyncing}";
        _lastSync.Text = $"Last Sync (UTC): {_viewModel.LastSyncUtc:O}";
        _queue.Text = $"Queue Length: {_viewModel.QueueLength}";
        _conflicts.Text = $"Conflicts: {_viewModel.ConflictCount}";
        _devices.Text = $"Connected Devices: {_viewModel.ConnectedDevices}";
    }
}
