$ErrorActionPreference = 'Stop'

$dbServer = '(localdb)\MSSQLLocalDB'
$dbName = 'Tabsan-EduSphere'
$superUsername = 'superadmin'
$adminUsername = 'admin.math'
$bootstrapPassword = 'EduSphere147'

function ConvertTo-SqlLiteral {
    param([string]$Value)
    return $Value.Replace("'", "''")
}

function Get-SqlScalar {
    param([string]$Query)

    $result = & sqlcmd -S $dbServer -d $dbName -E -h -1 -W -Q $Query
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed for query: $Query"
    }

    $text = ($result | Out-String).Trim()
    return $text
}

$originalSuperHash = $null

try {
    $originalSuperHash = Get-SqlScalar -Query "SET NOCOUNT ON; SELECT PasswordHash FROM users WHERE Username='$superUsername';"
    if ([string]::IsNullOrWhiteSpace($originalSuperHash)) {
        throw "SuperAdmin user '$superUsername' was not found."
    }

    $adminHash = Get-SqlScalar -Query "SET NOCOUNT ON; SELECT PasswordHash FROM users WHERE Username='$adminUsername';"
    if ([string]::IsNullOrWhiteSpace($adminHash)) {
        throw "Admin user '$adminUsername' was not found."
    }

    $escapedAdminHash = ConvertTo-SqlLiteral -Value $adminHash

    & sqlcmd -S $dbServer -d $dbName -E -Q "SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; UPDATE users SET IsLockedOut=0, FailedLoginAttempts=0, LockedOutUntil=NULL, MustChangePassword=0, PasswordHash='$escapedAdminHash' WHERE Username='$superUsername';"
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to bootstrap local SuperAdmin credentials.'
    }

    $superSecret = ConvertTo-SecureString -String $bootstrapPassword -AsPlainText -Force
    $adminSecret = ConvertTo-SecureString -String $bootstrapPassword -AsPlainText -Force

    & "$PSScriptRoot/verify-degree-rules-access.ps1" -SuperAdminSecret $superSecret -AdminSecret $adminSecret
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Verifier failed with exit code $exitCode."
    }
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($originalSuperHash)) {
        $escapedOriginalHash = ConvertTo-SqlLiteral -Value $originalSuperHash
        & sqlcmd -S $dbServer -d $dbName -E -Q "SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; UPDATE users SET PasswordHash='$escapedOriginalHash' WHERE Username='$superUsername';"
    }
}
