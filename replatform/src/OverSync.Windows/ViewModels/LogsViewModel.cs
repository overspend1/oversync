using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using OverSync.Windows.Services;

namespace OverSync.Windows.ViewModels;

public sealed partial class LogsViewModel : ObservableObject
{
    private readonly AppSessionState _session;

    public ObservableCollection<string> Entries { get; } = [];

    public LogsViewModel(AppSessionState session)
    {
        _session = session;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Entries.Clear();
        foreach (var entry in _session.Logs.Reverse())
        {
            Entries.Add(entry);
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"oversync-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
        await File.WriteAllLinesAsync(path, _session.Logs);
    }
}
