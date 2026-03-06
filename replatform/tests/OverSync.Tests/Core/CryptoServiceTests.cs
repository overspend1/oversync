using OverSync.Core.Models;
using OverSync.Core.Services;

namespace OverSync.Tests.Core;

public sealed class CryptoServiceTests
{
    [Fact]
    public async Task EncryptDecrypt_RoundTrip_Works()
    {
        var crypto = new CryptoService();
        var salt = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var key = await crypto.DeriveVaultKeyAsync("correct horse battery staple", salt);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("vault-content");

        var encrypted = crypto.Encrypt(plaintext, key);
        var decrypted = crypto.Decrypt(
            new EncryptedPayload(encrypted.Ciphertext, encrypted.Nonce, encrypted.Tag),
            key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task DeriveKey_SameInputs_ReturnsStableKey()
    {
        var crypto = new CryptoService();
        var salt = Enumerable.Range(0, 16).Select(i => (byte)(255 - i)).ToArray();

        var key1 = await crypto.DeriveVaultKeyAsync("passphrase", salt);
        var key2 = await crypto.DeriveVaultKeyAsync("passphrase", salt);

        Assert.Equal(key1, key2);
    }
}
