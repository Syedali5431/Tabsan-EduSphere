$ErrorActionPreference = 'Stop'

$superAdminSecret = Read-Host 'Enter SuperAdmin password' -AsSecureString
$adminSecret = Read-Host 'Enter Admin password (optional, press Enter for default)' -AsSecureString

$adminSecretText = [System.Net.NetworkCredential]::new('', $adminSecret).Password
if ([string]::IsNullOrWhiteSpace($adminSecretText)) {
    $adminSecret = $null
}

& "$PSScriptRoot/verify-degree-rules-access.ps1" -SuperAdminSecret $superAdminSecret -AdminSecret $adminSecret
