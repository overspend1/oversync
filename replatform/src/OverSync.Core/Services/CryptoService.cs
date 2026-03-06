using System.Security.Cryptography;
using System.Text.Json;
using Konscious.Security.Cryptography;
using OverSync.Core.Models;

namespace OverSync.Core.Services;

public sealed class CryptoService
{
    public async Task<byte[]> DeriveVaultKeyAsync(
        string passphrase,
        byte[] salt,
        int keyBytes = 32,
        CancellationToken cancellationToken = default)
    {
        var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(passphrase))
        {
            Salt = salt,
            Iterations = 4,
            MemorySize = 256 * 1024,
            DegreeOfParallelism = 4
        };

        cancellationToken.ThrowIfCancellationRequested();
        return await argon2.GetBytesAsync(keyBytes);
    }

    public EncryptedPayload Encrypt(byte[] plaintext, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("AesGcm requires a 256-bit key.", nameof(key));
        }

        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return new EncryptedPayload(ciphertext, nonce, tag);
    }

    public byte[] Decrypt(EncryptedPayload payload, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("AesGcm requires a 256-bit key.", nameof(key));
        }

        var plaintext = new byte[payload.Ciphertext.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext);
        return plaintext;
    }

    public byte[] SerializeEnvelope(EncryptedFileEnvelope envelope)
    {
        return JsonSerializer.SerializeToUtf8Bytes(envelope);
    }

    public EncryptedFileEnvelope DeserializeEnvelope(byte[] data)
    {
        var envelope = JsonSerializer.Deserialize<EncryptedFileEnvelope>(data);
        if (envelope is null)
        {
            throw new InvalidOperationException("Encrypted file envelope is invalid.");
        }

        return envelope;
    }
}
