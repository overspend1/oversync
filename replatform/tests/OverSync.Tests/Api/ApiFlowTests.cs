using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OverSync.Contracts;

namespace OverSync.Tests.Api;

public sealed class ApiFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiFlowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthAndSyncFlow_WorksEndToEnd()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequestDto(email, "Passw0rd!"));
        registerResponse.EnsureSuccessStatusCode();
        var tokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
        Assert.NotNull(tokens);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var vaultId = Guid.NewGuid();
        var deviceResponse = await _client.PostAsJsonAsync(
            "/v1/devices/register",
            new DeviceRegistrationRequestDto(vaultId, "test-device", "windows"));
        deviceResponse.EnsureSuccessStatusCode();

        var chunkBytes = System.Text.Encoding.UTF8.GetBytes("chunk-data");
        var putChunk = await _client.PutAsync(
            "/v1/sync/chunks/hash-1",
            new ByteArrayContent(chunkBytes));
        Assert.Equal(HttpStatusCode.Accepted, putChunk.StatusCode);

        var manifest = new ManifestDto(
            vaultId,
            1,
            DateTime.UtcNow,
            [
                new FileEntryDto(
                    "note.md",
                    "file-hash",
                    chunkBytes.Length,
                    DateTime.UtcNow,
                    1,
                    [new ChunkRefDto("hash-1", chunkBytes.Length, 0)])
            ]);

        var putManifest = await _client.PutAsJsonAsync("/v1/sync/manifest", new UploadManifestRequestDto(manifest));
        Assert.Equal(HttpStatusCode.Accepted, putManifest.StatusCode);

        var getManifest = await _client.GetAsync($"/v1/sync/manifest?vaultId={vaultId:D}");
        getManifest.EnsureSuccessStatusCode();
        var loadedManifest = await getManifest.Content.ReadFromJsonAsync<ManifestDto>();
        Assert.NotNull(loadedManifest);
        Assert.Equal(1, loadedManifest!.Version);

        var getChunk = await _client.GetAsync("/v1/sync/chunks/hash-1");
        getChunk.EnsureSuccessStatusCode();
        var loadedChunk = await getChunk.Content.ReadAsByteArrayAsync();
        Assert.Equal(chunkBytes, loadedChunk);

        var commit = await _client.PostAsJsonAsync("/v1/sync/commit", new CommitRequestDto(vaultId, "dev-1", 1));
        Assert.Equal(HttpStatusCode.Accepted, commit.StatusCode);
    }
}
