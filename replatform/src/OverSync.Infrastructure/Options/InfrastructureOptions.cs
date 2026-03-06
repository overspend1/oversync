namespace OverSync.Infrastructure.Options;

public sealed class InfrastructureOptions
{
    public bool UseInMemory { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
    public StorageOptions Storage { get; set; } = new();
}

public sealed class StorageOptions
{
    public bool UseS3 { get; set; }
    public string LocalRootPath { get; set; } = ".oversync-chunks";
    public string ServiceUrl { get; set; } = "http://localhost:9000";
    public string BucketName { get; set; } = "oversync-chunks";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public bool ForcePathStyle { get; set; } = true;
}
