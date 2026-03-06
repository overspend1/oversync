namespace OverSync.Api.Security;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed class PasswordService : IPasswordService
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
