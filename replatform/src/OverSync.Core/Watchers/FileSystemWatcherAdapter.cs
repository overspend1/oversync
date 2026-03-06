using System.Collections.Concurrent;
using OverSync.Core.Abstractions;
using OverSync.Core.Services;

namespace OverSync.Core.Watchers;

public sealed class FileSystemWatcherAdapter : IFileWatcherAdapter
{
    private readonly TimeSpan _debounceWindow;
    private readonly ConcurrentDictionary<string, DateTime> _pending = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _flushCts;

    public FileSystemWatcherAdapter(TimeSpan? debounceWindow = null)
    {
        _debounceWindow = debounceWindow ?? TimeSpan.FromMilliseconds(500);
    }

    public void Start(string path, Action<string> callback)
    {
        Stop();

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Vault path does not exist: {path}");
        }

        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        void Queue(string changedPath)
        {
            if (string.IsNullOrWhiteSpace(changedPath) || FileDiscovery.IsExcluded(changedPath))
            {
                return;
            }

            _pending[changedPath] = DateTime.UtcNow;
        }

        _watcher.Created += (_, args) => Queue(args.FullPath);
        _watcher.Changed += (_, args) => Queue(args.FullPath);
        _watcher.Renamed += (_, args) => Queue(args.FullPath);
        _watcher.Deleted += (_, args) => Queue(args.FullPath);

        _flushCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(_flushCts.Token))
            {
                var now = DateTime.UtcNow;
                foreach (var (changedPath, queuedAt) in _pending.ToArray())
                {
                    if (now - queuedAt >= _debounceWindow && _pending.TryRemove(changedPath, out _))
                    {
                        callback(changedPath);
                    }
                }
            }
        }, _flushCts.Token);
    }

    public void Stop()
    {
        _flushCts?.Cancel();
        _flushCts?.Dispose();
        _flushCts = null;

        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        _pending.Clear();
    }
}
