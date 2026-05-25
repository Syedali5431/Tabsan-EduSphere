namespace Tabsan.EduSphere.Web.Services;

public sealed record TwoFactorSetupApiModel(
    bool TwoFactorEnabled,
    string Issuer,
    string AccountName,
    string ManualKey,
    string ProvisioningUri,
    string QrCodeDataUrl);

public sealed record TwoFactorOperationResultApiModel(
    bool Success,
    bool TwoFactorEnabled,
    string Message);
