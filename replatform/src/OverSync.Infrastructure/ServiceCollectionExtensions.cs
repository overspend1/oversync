using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OverSync.Infrastructure.Abstractions;
using OverSync.Infrastructure.Options;
using OverSync.Infrastructure.Persistence;
using OverSync.Infrastructure.Stores;

namespace OverSync.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOverSyncInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InfrastructureOptions>(configuration.GetSection("Infrastructure"));
        var options = configuration.GetSection("Infrastructure").Get<InfrastructureOptions>() ?? new InfrastructureOptions();

        if (options.UseInMemory)
        {
            services.AddSingleton<IOverSyncMetadataStore, InMemoryMetadataStore>();
            services.AddSingleton<IChunkStorage, InMemoryChunkStorage>();
            return services;
        }

        services.AddDbContext<InfrastructureDbContext>(db =>
        {
            db.UseNpgsql(options.ConnectionString);
        });
        services.AddScoped<IOverSyncMetadataStore, EfCoreMetadataStore>();

        if (options.Storage.UseS3)
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = options.Storage.ServiceUrl,
                    ForcePathStyle = options.Storage.ForcePathStyle
                };
                return new AmazonS3Client(
                    new BasicAWSCredentials(options.Storage.AccessKey, options.Storage.SecretKey),
                    config);
            });
            services.AddSingleton<IChunkStorage>(_ => new S3ChunkStorage(
                _.GetRequiredService<IAmazonS3>(),
                options.Storage.BucketName));
        }
        else
        {
            services.AddSingleton<IChunkStorage>(_ => new LocalFileChunkStorage(options.Storage.LocalRootPath));
        }

        return services;
    }
}
