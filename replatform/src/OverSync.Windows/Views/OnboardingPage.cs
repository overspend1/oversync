using OverSync.Windows.ViewModels;

namespace OverSync.Windows.Views;

public sealed class OnboardingPage : Page
{
    private OnboardingViewModel? _viewModel;

    private readonly TextBox _vaultPath = new();
    private readonly TextBox _email = new();
    private readonly PasswordBox _password = new();
    private readonly PasswordBox _passphrase = new();
    private readonly TextBlock _error = new() { Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.IndianRed) };

    public OnboardingPage()
    {
        var layout = new StackPanel { Margin = new Thickness(24), Spacing = 10 };
        layout.Children.Add(new TextBlock { Text = "Onboarding", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        layout.Children.Add(new TextBlock { Text = "Vault Path" });
        layout.Children.Add(_vaultPath);
        layout.Children.Add(new TextBlock { Text = "Email" });
        layout.Children.Add(_email);
        layout.Children.Add(new TextBlock { Text = "Password" });
        layout.Children.Add(_password);
        layout.Children.Add(new TextBlock { Text = "Passphrase" });
        layout.Children.Add(_passphrase);

        var button = new Button { Content = "Initialize Vault Sync" };
        button.Click += async (_, _) => await InitializeAsync();
        layout.Children.Add(button);
        layout.Children.Add(_error);

        Content = new ScrollViewer { Content = layout };
        Loaded += (_, _) =>
        {
            _viewModel ??= App.Services.GetRequiredService<OnboardingViewModel>();
        };
    }

    private async Task InitializeAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.VaultPath = _vaultPath.Text;
        _viewModel.Email = _email.Text;
        _viewModel.Password = _password.Password;
        _viewModel.Passphrase = _passphrase.Password;
        await _viewModel.InitializeCommand.ExecuteAsync(null);
        _error.Text = _viewModel.ErrorMessage;
    }
}
