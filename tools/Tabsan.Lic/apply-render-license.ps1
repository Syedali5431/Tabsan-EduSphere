param(
    [string]$BaseUrl = "https://tabsan-edusphere.onrender.com",
    [string]$LicensePath = "tools/Tabsan.Lic/License/tabsan-license-e63eb7186fba4890a7963885de16de2a.tablic"
)

$ErrorActionPreference = "Stop"

if (-not [System.Uri]::IsWellFormedUriString($BaseUrl, [System.UriKind]::Absolute)) {
    throw "BaseUrl is invalid: $BaseUrl"
}

if (-not (Test-Path -Path $LicensePath)) {
    throw "License file not found: $LicensePath"
}

$Username = Read-Host "SuperAdmin username"
$PasswordSecure = Read-Host "SuperAdmin password" -AsSecureString
$MfaCode = Read-Host "MFA code (press Enter if not required)"

$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($PasswordSecure)
try {
    $Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}

$loginPayload = @{
    username = $Username
    password = $Password
    deviceInfo = "Tabsan.Lic apply-render-license"
}

if (-not [string]::IsNullOrWhiteSpace($MfaCode)) {
    $loginPayload.mfaCode = $MfaCode
}

$loginJson = $loginPayload | ConvertTo-Json -Compress
$loginUrl = ($BaseUrl.TrimEnd('/')) + "/api/v1/auth/login"

Write-Host "Logging in to $loginUrl ..."

try {
    $loginResponse = Invoke-RestMethod -Method Post -Uri $loginUrl -ContentType "application/json" -Body $loginJson
}
catch {
    $statusCode = $null
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if ($statusCode -eq 428) {
        throw "MFA is required. Re-run and provide MFA code when prompted."
    }

    throw "Login failed. Status: $statusCode. $($_.Exception.Message)"
}

$accessToken = $loginResponse.accessToken
if ([string]::IsNullOrWhiteSpace($accessToken)) {
    throw "Login succeeded but accessToken was missing in response."
}

$uploadUrl = ($BaseUrl.TrimEnd('/')) + "/api/v1/license/upload"
$statusUrl = ($BaseUrl.TrimEnd('/')) + "/api/v1/license/status"

$http = New-Object System.Net.Http.HttpClient
$fileStream = $null

try {
    $http.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $accessToken)

    $fileStream = [System.IO.File]::OpenRead((Resolve-Path $LicensePath))
    $fileName = [System.IO.Path]::GetFileName($LicensePath)

    $multipart = New-Object System.Net.Http.MultipartFormDataContent
    $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
    $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")
    $multipart.Add($fileContent, "file", $fileName)

    Write-Host "Uploading license file $fileName to $uploadUrl ..."

    $uploadResponse = $http.PostAsync($uploadUrl, $multipart).GetAwaiter().GetResult()
    $uploadBody = $uploadResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    if (-not $uploadResponse.IsSuccessStatusCode) {
        throw "Upload failed. Status: $([int]$uploadResponse.StatusCode). Body: $uploadBody"
    }

    Write-Host "Upload success: $uploadBody"

    Write-Host "Fetching license status from $statusUrl ..."
    $statusResponse = Invoke-RestMethod -Method Get -Uri $statusUrl -Headers @{ Authorization = "Bearer $accessToken" }

    Write-Host "License status response:"
    $statusResponse | ConvertTo-Json -Depth 5
}
finally {
    if ($fileStream) { $fileStream.Dispose() }
    if ($http) { $http.Dispose() }
}
