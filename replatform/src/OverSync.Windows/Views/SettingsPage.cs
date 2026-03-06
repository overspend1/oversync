using OverSync.Windows.ViewModels;

namespace OverSync.Windows.Views;

public sealed class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;
    private readonly TextBox _apiBaseUrl = new();
    private readonly NumberBox _syncInterval = new() { Minimum = 5 };
    private readonly TextBlock _status = new();

    public SettingsPage()
    {
        var layout = new StackPanel { Margin = new Thickness(24), Spacing = 10 };
        layout.Children.Add(new TextBlock { Text = "Settings", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        layout.Children.Add(new TextBlock { Text = "API Base URL" });
        layout.Children.Add(_apiBaseUrl);
        layout.Children.Add(new TextBlock { Text = "Sync Interval (seconds)" });
        layout.Children.Add(_syncInterval);
        var saveButton = new Button { Content = "Save", HorizontalAlignment = HorizontalAlignment.Left };
        saveButton.Click += (_, _) => Save();
        layout.Children.Add(saveButton);
        layout.Children.Add(_status);

        Content = layout;
        Loaded += (_, _) =>
        {
            _viewModel ??= App.Services.GetRequiredService<SettingsViewModel>();
            _apiBaseUrl.Text = _viewModel.ApiBaseUrl;
            _syncInterval.Value = _viewModel.SyncIntervalSeconds;
            _status.Text = _viewModel.StatusMessage;
        };
    }

    private void Save()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.ApiBaseUrl = _apiBaseUrl.Text;
        _viewModel.SyncIntervalSeconds = (int)_syncInterval.Value;
        _viewModel.SaveCommand.Execute(null);
        _status.Text = _viewModel.StatusMessage;
    }
}
