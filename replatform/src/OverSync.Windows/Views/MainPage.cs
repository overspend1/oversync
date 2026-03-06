using Microsoft.UI.Xaml.Controls.Primitives;
using OverSync.Windows.ViewModels;

namespace OverSync.Windows.Views;

public sealed class MainPage : Page
{
    private readonly NavigationView _nav;
    private readonly ContentControl _host;
    private MainShellViewModel? _viewModel;
    private readonly AppBarButton _pauseResumeButton;

    public MainPage()
    {
        _pauseResumeButton = new AppBarButton { Label = "Pause Sync" };
        _pauseResumeButton.Click += (_, _) => _viewModel?.PauseOrResumeCommand.Execute(null);

        var forceSyncButton = new AppBarButton { Label = "Force Sync" };
        forceSyncButton.Click += (_, _) => _viewModel?.ForceSyncCommand.Execute(null);

        var minimizeButton = new AppBarButton { Label = "Minimize" };
        minimizeButton.Click += (_, _) => _viewModel?.MinimizeToTrayCommand.Execute(null);

        var quitButton = new AppBarButton { Label = "Quit" };
        quitButton.Click += (_, _) => _viewModel?.QuitCommand.Execute(null);

        var commandBar = new CommandBar();
        commandBar.PrimaryCommands.Add(_pauseResumeButton);
        commandBar.PrimaryCommands.Add(forceSyncButton);
        commandBar.PrimaryCommands.Add(minimizeButton);
        commandBar.PrimaryCommands.Add(quitButton);

        _host = new ContentControl();
        _nav = new NavigationView
        {
            Header = commandBar,
            Content = _host
        };
        _nav.MenuItems.Add(new NavigationViewItem { Content = "Onboarding", Tag = "onboarding" });
        _nav.MenuItems.Add(new NavigationViewItem { Content = "Dashboard", Tag = "dashboard" });
        _nav.MenuItems.Add(new NavigationViewItem { Content = "Settings", Tag = "settings" });
        _nav.MenuItems.Add(new NavigationViewItem { Content = "Logs", Tag = "logs" });
        _nav.SelectionChanged += OnSelectionChanged;

        Content = _nav;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel ??= App.Services.GetRequiredService<MainShellViewModel>();
        _viewModel.PropertyChanged += (_, _) => RefreshHeader();
        RefreshHeader();

        var target = _viewModel.IsOnboarded ? "dashboard" : "onboarding";
        Navigate(target);
        _nav.SelectedItem = _nav.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), target, StringComparison.OrdinalIgnoreCase));
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            Navigate(item.Tag?.ToString() ?? "onboarding");
        }
    }

    private void RefreshHeader()
    {
        if (_viewModel is null)
        {
            return;
        }

        _pauseResumeButton.Label = _viewModel.PauseResumeText;
    }

    private void Navigate(string tag)
    {
        _host.Content = tag switch
        {
            "dashboard" => new DashboardPage(),
            "settings" => new SettingsPage(),
            "logs" => new LogsPage(),
            _ => new OnboardingPage()
        };
    }
}
