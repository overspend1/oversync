namespace OverSync.Contracts;

public sealed record RegisterRequestDto(string Email, string Password);

public sealed record LoginRequestDto(string Email, string Password);

public sealed record RefreshRequestDto(string RefreshToken);

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc);

public sealed record UserDto(
    Guid UserId,
    string Email,
    DateTime CreatedAtUtc);
