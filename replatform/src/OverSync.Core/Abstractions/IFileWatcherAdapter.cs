namespace OverSync.Core.Abstractions;

public interface IFileWatcherAdapter
{
    void Start(string path, Action<string> callback);
    void Stop();
}
