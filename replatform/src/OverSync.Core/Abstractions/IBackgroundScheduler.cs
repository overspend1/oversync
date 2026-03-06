namespace OverSync.Core.Abstractions;

public interface IBackgroundScheduler
{
    Task StartAsync(
        TimeSpan interval,
        Func<CancellationToken, Task> callback,
        CancellationToken cancellationToken = default);

    Task StopAsync();
}
