using System.Security.Cryptography;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Security;

/// <summary>
/// Phase 5: AES-256-CBC encryption with PBKDF2 key derivation.
/// Protected data format: Base64(salt[16] + IV[16] + ciphertext).
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _masterKey;

    /// <summary>
    /// Initializes the encryption service with a master key.
    /// In production, this should come from a secure key vault or environment variable.
    /// </summary>
    public EncryptionService(string masterKey)
    {
        _masterKey = System.Text.Encoding.UTF8.GetBytes(masterKey);
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));

        var salt = RandomNumberGenerator.GetBytes(16);
        using var derive = new Rfc2898DeriveBytes(_masterKey, salt, 100_000, HashAlgorithmName.SHA256);
        var key = derive.GetBytes(32); // AES-256

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        var iv = aes.IV;

        using var ms = new MemoryStream();
        ms.Write(salt, 0, salt.Length);
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plaintext);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));

        var data = Convert.FromBase64String(ciphertext);
        if (data.Length < 32) throw new ArgumentException("Invalid ciphertext format.", nameof(ciphertext));

        var salt = data[..16];
        var iv = data[16..32];

        using var derive = new Rfc2898DeriveBytes(_masterKey, salt, 100_000, HashAlgorithmName.SHA256);
        var key = derive.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream(data, 32, data.Length - 32);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
