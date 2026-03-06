namespace OverSync.Windows.Services;

public sealed class WindowControlService : IWindowControlService
{
    private readonly Func<Window?> _windowAccessor;

    public WindowControlService(Func<Window?> windowAccessor)
    {
        _windowAccessor = windowAccessor;
    }

    public void MinimizeToBackground()
    {
        var window = _windowAccessor();
        window?.AppWindow.Hide();
    }

    public void Restore()
    {
        var window = _windowAccessor();
        window?.AppWindow.Show();
        window?.Activate();
    }

    public void Quit()
    {
        Application.Current.Exit();
    }
}
