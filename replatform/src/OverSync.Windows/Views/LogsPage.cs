using OverSync.Windows.ViewModels;

namespace OverSync.Windows.Views;

public sealed class LogsPage : Page
{
    private LogsViewModel? _viewModel;
    private readonly ListView _list = new();

    public LogsPage()
    {
        var root = new Grid { Margin = new Thickness(24), RowSpacing = 10 };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(new TextBlock { Text = "Logs", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var refresh = new Button { Content = "Refresh" };
        refresh.Click += (_, _) => Refresh();
        header.Children.Add(refresh);

        var export = new Button { Content = "Export" };
        export.Click += async (_, _) => await ExportAsync();
        header.Children.Add(export);

        Grid.SetRow(header, 0);
        root.Children.Add(header);

        Grid.SetRow(_list, 1);
        root.Children.Add(_list);
        Content = root;

        Loaded += (_, _) =>
        {
            _viewModel ??= App.Services.GetRequiredService<LogsViewModel>();
            Refresh();
        };
    }

    private void Refresh()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.RefreshCommand.Execute(null);
        _list.ItemsSource = _viewModel.Entries;
    }

    private async Task ExportAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.ExportCommand.ExecuteAsync(null);
    }
}
