namespace Tabsan.EduSphere.Infrastructure.Licensing;

/// <summary>
/// Compile-time embedded cryptographic key material used by EduSphere to verify and
/// decrypt incoming .tablic license files.
///
/// Only the RSA PUBLIC key and the AES-256 key live here.
/// The RSA private key exists solely in the Tabsan-Lic standalone tool.
/// </summary>
internal static class EmbeddedKeys
{
    /// <summary>
    /// RSA-2048 public key in PKCS#1 PEM format.
    /// Used to verify the RSA-2048 signature embedded in every .tablic file.
    /// </summary>
    internal const string RsaPublicKeyPem =
        """
        -----BEGIN RSA PUBLIC KEY-----
        MIIBCgKCAQEA3FADQRmkWQRaex3/Ytz9bsjLS7+cehMwwPDMeCN0cCvfpzfoQI+bqQS3qFyeEqYV
        mqAnoA7P26ZRktMSl+bxzcSpdnO7pIazvjQ+dm7JNRv3QtQm6n1nQkqEaNo4tQOpZZ7o81eHV8vC
        /uFwCjz5Ezp5+173fEoo8AEdVw9tqq8+ot+pBXHt/EYplP+FmZwo9SDkJTeeofSqVjV6PKkCvmCO
        zWzWzh7+McmeOYdtrA06iIA9YGjfl4BNZaRhXS1gj5AEknVGDKJCUtVoPvjT6MpuaCvKNbrlRPCA
        0Pmg+Icflwx7VCTAPNQvSBxa78v3JCZATBlNwmi4lF9VyvMqwQIDAQAB
        -----END RSA PUBLIC KEY-----
        """;

    /// <summary>
    /// AES-256 symmetric key shared with Tabsan-Lic (Base64-encoded, 32 bytes).
    /// Used to decrypt the AES-256-CBC encrypted payload inside a .tablic file.
    /// </summary>
    internal const string AesKeyBase64 = "K1LFlYZwqwNp3lhFAur7vASzMGyInHpjnVhCXpceXBc=";
}
