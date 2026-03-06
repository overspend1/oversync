namespace OverSync.Windows.Services;

public interface IWindowControlService
{
    void MinimizeToBackground();
    void Restore();
    void Quit();
}
