param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = "",
    [switch]$FailOnIssues
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-JsonValue {
    param(
        [Parameter(Mandatory)]$Object,
        [Parameter(Mandatory)][string]$Path
    )

    $current = $Object
    foreach ($segment in $Path.Split(':')) {
        if ($null -eq $current) { return $null }

        if ($current -is [System.Collections.IDictionary]) {
            if (-not $current.Contains($segment)) { return $null }
            $current = $current[$segment]
            continue
        }

        $property = $current.PSObject.Properties[$segment]
        if ($null -eq $property) { return $null }
        $current = $property.Value
    }

    return $current
}

function Is-UnsafePlaceholder {
    param([AllowNull()][string]$Value)

    $incompleteMarker = 'to' + 'do'

    if ([string]::IsNullOrWhiteSpace($Value)) { return $true }
    $normalized = $Value.Trim().ToLowerInvariant()

    return $normalized.Contains("replace_with") -or
           $normalized.Contains("or_set_via_env_var") -or
           $normalized.Contains("change_me") -or
           $normalized.Contains("changeme") -or
           $normalized.Contains($incompleteMarker) -or
           $normalized.Contains("yourdomain.com") -or
           $normalized.Contains("example.com") -or
           $normalized.Contains("<") -or
           $normalized.Contains(">")
}

function Add-Result {
    param(
        [string]$Check,
        [string]$Status,
        [string]$Details
    )

    $script:Results.Add([pscustomobject]@{
        Check   = $Check
        Status  = $Status
        Details = $Details
    })
}

$Results = [System.Collections.Generic.List[object]]::new()

$configMap = @(
    @{ Name = "API"; Files = @("src/Tabsan.EduSphere.API/appsettings.json", "src/Tabsan.EduSphere.API/appsettings.Development.json", "src/Tabsan.EduSphere.API/appsettings.Production.json") },
    @{ Name = "Web"; Files = @("src/Tabsan.EduSphere.Web/appsettings.json", "src/Tabsan.EduSphere.Web/appsettings.Development.json", "src/Tabsan.EduSphere.Web/appsettings.Production.json") },
    @{ Name = "BackgroundJobs"; Files = @("src/Tabsan.EduSphere.BackgroundJobs/appsettings.json", "src/Tabsan.EduSphere.BackgroundJobs/appsettings.Development.json", "src/Tabsan.EduSphere.BackgroundJobs/appsettings.Production.json") }
)

$parityChecks = @{
    "API" = @("ConnectionStrings:DefaultConnection", "JwtSettings:SecretKey", "ScaleOut:RedisConnectionString", "QueuePlatform:Provider", "MediaStorage:Provider", "MediaStorage:SignedUrlSecret")
    "Web" = @("EduApi:BaseUrl", "ScaleOut:SharedDataProtectionKeyRingPath")
    "BackgroundJobs" = @("ConnectionStrings:DefaultConnection")
}

$documents = @{}

foreach ($service in $configMap) {
    foreach ($relativeFile in $service.Files) {
        $fullPath = Join-Path $RepoRoot $relativeFile
        if (-not (Test-Path -LiteralPath $fullPath)) {
            Add-Result -Check "FileExists:$relativeFile" -Status "FAIL" -Details "Missing configuration file"
            continue
        }

        $raw = Get-Content -LiteralPath $fullPath -Raw
        $documents[$relativeFile] = $raw | ConvertFrom-Json
        Add-Result -Check "FileExists:$relativeFile" -Status "PASS" -Details "Found"
    }
}

foreach ($service in $configMap) {
    $serviceName = $service.Name
    $keys = $parityChecks[$serviceName]
    foreach ($key in $keys) {
        foreach ($relativeFile in $service.Files) {
            if (-not $documents.ContainsKey($relativeFile)) { continue }

            $value = Get-JsonValue -Object $documents[$relativeFile] -Path $key
            $isDevelopmentFile = $relativeFile.IndexOf(".Development.", [StringComparison]::OrdinalIgnoreCase) -ge 0
            if ($null -eq $value) {
                if ($isDevelopmentFile) {
                    Add-Result -Check "Parity:${serviceName}:$key" -Status "PASS" -Details "$relativeFile missing key (inherited from base appsettings)"
                }
                else {
                    Add-Result -Check "Parity:${serviceName}:$key" -Status "FAIL" -Details "$relativeFile missing key"
                }
            }
            else {
                Add-Result -Check "Parity:${serviceName}:$key" -Status "PASS" -Details "$relativeFile contains key"
            }
        }
    }
}

$prodSecretChecks = @(
    @{ Label = "API DefaultConnection"; File = "src/Tabsan.EduSphere.API/appsettings.Production.json"; Path = "ConnectionStrings:DefaultConnection"; Env = "ConnectionStrings__DefaultConnection"; Required = $true },
    @{ Label = "API JWT Secret"; File = "src/Tabsan.EduSphere.API/appsettings.Production.json"; Path = "JwtSettings:SecretKey"; Env = "JwtSettings__SecretKey"; Required = $true },
    @{ Label = "API SMTP Username"; File = "src/Tabsan.EduSphere.API/appsettings.Production.json"; Path = "Email:Username"; Env = "Email__Username"; Required = $true },
    @{ Label = "API SMTP Password"; File = "src/Tabsan.EduSphere.API/appsettings.Production.json"; Path = "Email:Password"; Env = "Email__Password"; Required = $true },
    @{ Label = "API Redis Connection"; File = "src/Tabsan.EduSphere.API/appsettings.Production.json"; Path = "ScaleOut:RedisConnectionString"; Env = "ScaleOut__RedisConnectionString"; Required = $true },
    @{ Label = "API Media Signed URL Secret"; File = "src/Tabsan.EduSphere.API/appsettings.Production.json"; Path = "MediaStorage:SignedUrlSecret"; Env = "MediaStorage__SignedUrlSecret"; Required = $true },
    @{ Label = "Web Shared Key Ring Path"; File = "src/Tabsan.EduSphere.Web/appsettings.Production.json"; Path = "ScaleOut:SharedDataProtectionKeyRingPath"; Env = "ScaleOut__SharedDataProtectionKeyRingPath"; Required = $true },
    @{ Label = "Web API Base URL"; File = "src/Tabsan.EduSphere.Web/appsettings.Production.json"; Path = "EduApi:BaseUrl"; Env = "EduApi__BaseUrl"; Required = $true },
    @{ Label = "BackgroundJobs DefaultConnection"; File = "src/Tabsan.EduSphere.BackgroundJobs/appsettings.Production.json"; Path = "ConnectionStrings:DefaultConnection"; Env = "ConnectionStrings__DefaultConnection"; Required = $true }
)

foreach ($check in $prodSecretChecks) {
    if (-not $documents.ContainsKey($check.File)) {
        Add-Result -Check "Secret:$($check.Label)" -Status "FAIL" -Details "Missing production config file"
        continue
    }

    $fileValue = Get-JsonValue -Object $documents[$check.File] -Path $check.Path
    $envValue = [Environment]::GetEnvironmentVariable($check.Env)
    $effective = if ([string]::IsNullOrWhiteSpace($envValue)) { [string]$fileValue } else { $envValue }

    if ($check.Required -and (Is-UnsafePlaceholder -Value $effective)) {
        $effectiveSource = if ([string]::IsNullOrWhiteSpace($envValue)) { "file" } else { "env" }
        Add-Result -Check "Secret:$($check.Label)" -Status "FAIL" -Details "Unsafe placeholder or empty value (effective source: $effectiveSource)"
    }
    else {
        Add-Result -Check "Secret:$($check.Label)" -Status "PASS" -Details "Effective value appears configured"
    }
}

$prodApi = $documents["src/Tabsan.EduSphere.API/appsettings.Production.json"]
if ($null -ne $prodApi) {
    $provider = [string](Get-JsonValue -Object $prodApi -Path "QueuePlatform:Provider")
    $rabbitEnabled = [bool](Get-JsonValue -Object $prodApi -Path "QueuePlatform:RabbitMq:Enabled")
    $requiresRabbit = $rabbitEnabled -or $provider.Equals("RabbitMq", [StringComparison]::OrdinalIgnoreCase)

    if ($requiresRabbit) {
        $envValue = [Environment]::GetEnvironmentVariable("QueuePlatform__RabbitMq__ConnectionString")
        $fileValue = [string](Get-JsonValue -Object $prodApi -Path "QueuePlatform:RabbitMq:ConnectionString")
        $effective = if ([string]::IsNullOrWhiteSpace($envValue)) { $fileValue } else { $envValue }
        if (Is-UnsafePlaceholder -Value $effective) {
            Add-Result -Check "Secret:API RabbitMq Connection" -Status "FAIL" -Details "RabbitMq is enabled/selected but connection string is unsafe"
        }
        else {
            Add-Result -Check "Secret:API RabbitMq Connection" -Status "PASS" -Details "RabbitMq connection string appears configured"
        }
    }
    else {
        Add-Result -Check "Secret:API RabbitMq Connection" -Status "PASS" -Details "RabbitMq not required for current provider selection"
    }
}

$failCount = ($Results | Where-Object { $_.Status -eq "FAIL" } | Measure-Object).Count
$passCount = ($Results | Where-Object { $_.Status -eq "PASS" } | Measure-Object).Count

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $RepoRoot "Artifacts/Phase36/Stage36.2/Environment-Readiness-$timestamp.md"
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# Stage 36.2 Environment and Secret Readiness Report")
$lines.Add("")
$utcStamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")
$lines.Add("- Generated (UTC): $utcStamp")
$lines.Add("- Repository root: $RepoRoot")
$lines.Add("- Pass checks: $passCount")
$lines.Add("- Fail checks: $failCount")
$lines.Add("")
$lines.Add("| Check | Status | Details |")
$lines.Add("|---|---|---|")
foreach ($item in $Results) {
    $details = ($item.Details -replace "\|", "\\|")
    $lines.Add("| $($item.Check) | $($item.Status) | $details |")
}

Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
Write-Host "Stage 36.2 readiness report written: $OutputPath"

if ($FailOnIssues -and $failCount -gt 0) {
    throw "Environment readiness validation failed with $failCount failing checks."
}