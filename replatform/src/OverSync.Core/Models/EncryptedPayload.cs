namespace OverSync.Core.Models;

public sealed record EncryptedPayload(
    byte[] Ciphertext,
    byte[] Nonce,
    byte[] Tag);

public sealed record EncryptedFileEnvelope(
    string Path,
    string Hash,
    byte[] Nonce,
    byte[] Tag,
    byte[] Ciphertext);
