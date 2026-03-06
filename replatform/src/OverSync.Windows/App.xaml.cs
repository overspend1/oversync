using Microsoft.Extensions.DependencyInjection;
using OverSync.Core;
using OverSync.Core.Abstractions;
using OverSync.Windows.Services;
using OverSync.Windows.ViewModels;
using OverSync.Windows.Views;

namespace OverSync.Windows;

public sealed class App : Application
{
    private Window? _window;

    public App()
    {
        Services = ConfigureServices();
    }

    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window ??= Services.GetRequiredService<MainWindow>();
        _window.Activate();

        if (args.Arguments.Contains("minimized", StringComparison.OrdinalIgnoreCase))
        {
            Services.GetRequiredService<IWindowControlService>().MinimizeToBackground();
        }
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        var session = new AppSessionState();
        services.AddSingleton(session);
        services.AddSingleton<ISecretStore, DpapiSecretStore>();
        services.AddSingleton<IWindowControlService>(_ => new WindowControlService(() => _window));

        var apiBase = new Uri(session.ApiBaseUrl);
        services.AddOverSyncCore(apiBase);

        services.AddSingleton<SyncOrchestratorService>();

        services.AddSingleton<MainShellViewModel>();
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LogsViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<MainPage>();
        services.AddTransient<OnboardingPage>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<LogsPage>();

        return services.BuildServiceProvider();
    }
}
