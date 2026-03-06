using OverSync.Contracts;

namespace OverSync.Core.Abstractions;

public interface ISyncApiClient
{
    Task<AuthTokenDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default);

    Task<DeviceDto> RegisterDeviceAsync(
        DeviceRegistrationRequestDto request,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<ManifestDto?> GetManifestAsync(
        Guid vaultId,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task UploadManifestAsync(
        UploadManifestRequestDto request,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task UploadChunkAsync(
        string hash,
        Stream content,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadChunkAsync(
        string hash,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task CommitAsync(
        CommitRequestDto request,
        string accessToken,
        CancellationToken cancellationToken = default);
}
