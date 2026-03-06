using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OverSync.Api.Security;
using OverSync.Contracts;
using OverSync.Infrastructure;
using OverSync.Infrastructure.Abstractions;
using OverSync.Infrastructure.Options;
using OverSync.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<InfrastructureOptions>(builder.Configuration.GetSection("Infrastructure"));
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddOverSyncInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var infrastructureOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<InfrastructureOptions>>().Value;
if (!infrastructureOptions.UseInMemory)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/v1/auth/register", async (
    RegisterRequestDto request,
    IOverSyncMetadataStore store,
    IPasswordService passwordService,
    ITokenService tokenService,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions,
    CancellationToken cancellationToken) =>
{
    var existing = await store.GetUserByEmailAsync(request.Email, cancellationToken);
    if (existing is not null)
    {
        return Results.Conflict(new { message = "User already exists." });
    }

    var user = await store.CreateUserAsync(request.Email, passwordService.Hash(request.Password), cancellationToken);
    var tokens = tokenService.CreateTokenPair(user.UserId);
    var refreshExpiry = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);
    await store.CreateRefreshTokenAsync(user.UserId, tokens.RefreshToken, refreshExpiry, cancellationToken);
    return Results.Ok(tokens);
});

app.MapPost("/v1/auth/login", async (
    LoginRequestDto request,
    IOverSyncMetadataStore store,
    IPasswordService passwordService,
    ITokenService tokenService,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions,
    CancellationToken cancellationToken) =>
{
    var user = await store.GetUserByEmailAsync(request.Email, cancellationToken);
    if (user is null || !passwordService.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var tokens = tokenService.CreateTokenPair(user.UserId);
    var refreshExpiry = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);
    await store.CreateRefreshTokenAsync(user.UserId, tokens.RefreshToken, refreshExpiry, cancellationToken);
    return Results.Ok(tokens);
});

app.MapPost("/v1/auth/refresh", async (
    RefreshRequestDto request,
    IOverSyncMetadataStore store,
    ITokenService tokenService,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions,
    CancellationToken cancellationToken) =>
{
    var token = await store.GetRefreshTokenAsync(request.RefreshToken, cancellationToken);
    if (token is null || token.IsRevoked || token.ExpiresAtUtc <= DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    var next = tokenService.CreateTokenPair(token.UserId);
    await store.RotateRefreshTokenAsync(
        request.RefreshToken,
        next.RefreshToken,
        DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays),
        cancellationToken);

    return Results.Ok(next);
});

app.MapPost("/v1/devices/register", [Authorize] async (
    DeviceRegistrationRequestDto request,
    HttpContext httpContext,
    IOverSyncMetadataStore store,
    CancellationToken cancellationToken) =>
{
    var userId = httpContext.User.GetUserId();
    var deviceId = $"dev-{Guid.NewGuid():N}";
    var device = await store.RegisterDeviceAsync(userId, deviceId, request, cancellationToken);
    return Results.Ok(device);
});

app.MapGet("/v1/sync/manifest", [Authorize] async (
    Guid vaultId,
    IOverSyncMetadataStore store,
    CancellationToken cancellationToken) =>
{
    var manifest = await store.GetManifestAsync(vaultId, cancellationToken);
    return manifest is null ? Results.NotFound() : Results.Ok(manifest);
});

app.MapPut("/v1/sync/manifest", [Authorize] async (
    UploadManifestRequestDto request,
    HttpContext httpContext,
    IOverSyncMetadataStore store,
    CancellationToken cancellationToken) =>
{
    var userId = httpContext.User.GetUserId();
    await store.SaveManifestAsync(userId, request.Manifest, cancellationToken);
    return Results.Accepted();
});

app.MapPut("/v1/sync/chunks/{hash}", [Authorize] async (
    string hash,
    HttpRequest request,
    IChunkStorage chunkStorage,
    CancellationToken cancellationToken) =>
{
    await chunkStorage.StoreAsync(hash, request.Body, cancellationToken);
    return Results.Accepted();
});

app.MapGet("/v1/sync/chunks/{hash}", [Authorize] async (
    string hash,
    IChunkStorage chunkStorage,
    CancellationToken cancellationToken) =>
{
    var chunk = await chunkStorage.ReadAsync(hash, cancellationToken);
    if (chunk is null)
    {
        return Results.NotFound();
    }

    return Results.File(chunk, "application/octet-stream");
});

app.MapPost("/v1/sync/commit", [Authorize] async (
    CommitRequestDto request,
    HttpContext httpContext,
    IOverSyncMetadataStore store,
    CancellationToken cancellationToken) =>
{
    var userId = httpContext.User.GetUserId();
    await store.SaveCommitAsync(userId, request.VaultId, request.DeviceId, request.Version, cancellationToken);
    return Results.Accepted();
});

app.Run();

public partial class Program
{
}
