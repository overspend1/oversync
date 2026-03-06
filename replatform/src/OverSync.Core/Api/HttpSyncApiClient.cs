using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OverSync.Contracts;
using OverSync.Core.Abstractions;

namespace OverSync.Core.Api;

public sealed class HttpSyncApiClient : ISyncApiClient
{
    private readonly HttpClient _httpClient;

    public HttpSyncApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<AuthTokenDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        return SendJsonAsync<AuthTokenDto>(HttpMethod.Post, "/v1/auth/register", request, cancellationToken);
    }

    public Task<AuthTokenDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        return SendJsonAsync<AuthTokenDto>(HttpMethod.Post, "/v1/auth/login", request, cancellationToken);
    }

    public Task<AuthTokenDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default)
    {
        return SendJsonAsync<AuthTokenDto>(HttpMethod.Post, "/v1/auth/refresh", request, cancellationToken);
    }

    public Task<DeviceDto> RegisterDeviceAsync(
        DeviceRegistrationRequestDto request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendJsonAsync<DeviceDto>(HttpMethod.Post, "/v1/devices/register", request, cancellationToken, accessToken);
    }

    public async Task<ManifestDto?> GetManifestAsync(
        Guid vaultId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(
            HttpMethod.Get,
            $"/v1/sync/manifest?vaultId={vaultId:D}",
            accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ManifestDto>(cancellationToken);
    }

    public async Task UploadManifestAsync(
        UploadManifestRequestDto request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _ = await SendJsonAsync<object>(HttpMethod.Put, "/v1/sync/manifest", request, cancellationToken, accessToken);
    }

    public async Task UploadChunkAsync(
        string hash,
        Stream content,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(HttpMethod.Put, $"/v1/sync/chunks/{hash}", accessToken);
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadChunkAsync(
        string hash,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(HttpMethod.Get, $"/v1/sync/chunks/{hash}", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task CommitAsync(
        CommitRequestDto request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _ = await SendJsonAsync<object>(HttpMethod.Post, "/v1/sync/commit", request, cancellationToken, accessToken);
    }

    private async Task<T> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken,
        string? accessToken = null)
    {
        using var request = BuildRequest(method, path, accessToken);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (typeof(T) == typeof(object))
        {
            return default!;
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("API returned an empty response.");
        }

        return result;
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, string? accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
    }
}
