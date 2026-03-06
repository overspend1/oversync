namespace OverSync.Api.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "oversync-api";
    public string Audience { get; set; } = "oversync-clients";
    public string SigningKey { get; set; } = "dev-only-insecure-signing-key-change-me-now";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
