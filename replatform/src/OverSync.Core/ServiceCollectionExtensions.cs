using Microsoft.Extensions.DependencyInjection;
using OverSync.Core.Abstractions;
using OverSync.Core.Api;
using OverSync.Core.Scheduling;
using OverSync.Core.Services;
using OverSync.Core.State;
using OverSync.Core.Sync;
using OverSync.Core.Watchers;

namespace OverSync.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOverSyncCore(this IServiceCollection services, Uri apiBaseUri)
    {
        services.AddSingleton<CryptoService>();
        services.AddSingleton<IFileWatcherAdapter, FileSystemWatcherAdapter>();
        services.AddSingleton<IBackgroundScheduler, TimerBackgroundScheduler>();
        services.AddSingleton<ISyncStateStore, SqliteSyncStateStore>();
        services.AddSingleton<ISyncEngine, SyncEngine>();

        services.AddHttpClient<ISyncApiClient, HttpSyncApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
