using OverSync.Core.Abstractions;

namespace OverSync.Core.Scheduling;

public sealed class TimerBackgroundScheduler : IBackgroundScheduler
{
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    public Task StartAsync(
        TimeSpan interval,
        Func<CancellationToken, Task> callback,
        CancellationToken cancellationToken = default)
    {
        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(_loopCts.Token))
            {
                await callback(_loopCts.Token);
            }
        }, _loopCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_loopCts is null)
        {
            return;
        }

        _loopCts.Cancel();
        if (_loopTask is not null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        _loopCts.Dispose();
        _loopCts = null;
        _loopTask = null;
    }
}
