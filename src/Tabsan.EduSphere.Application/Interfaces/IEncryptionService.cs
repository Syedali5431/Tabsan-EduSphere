namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Phase 5: Symmetric encryption service for protecting sensitive data at rest.
/// Uses AES-256-CBC with PBKDF2 key derivation.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypts a plaintext string. Returns Base64-encoded ciphertext with embedded IV.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a Base64-encoded ciphertext back to plaintext.</summary>
    string Decrypt(string ciphertext);
}
