using Amazon.S3;
using Amazon.S3.Model;
using OverSync.Infrastructure.Abstractions;

namespace OverSync.Infrastructure.Stores;

public sealed class S3ChunkStorage : IChunkStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3ChunkStorage(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public async Task StoreAsync(string hash, Stream content, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = hash,
            InputStream = content
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, hash, cancellationToken);
            await using var stream = response.ResponseStream;
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
