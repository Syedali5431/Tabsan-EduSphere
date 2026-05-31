param(
    [string]$WebBaseUrl = 'http://localhost:5063',
    [string]$ApiBaseUrl = 'http://localhost:5181',
    [string]$SuperAdminUsername = 'superadmin',
    [securestring]$SuperAdminSecret,
    [string]$AdminUsername = 'admin.math',
    [securestring]$AdminSecret
)

$ErrorActionPreference = 'Stop'

if ($null -eq $SuperAdminSecret -and -not [string]::IsNullOrWhiteSpace($env:EDUSPHERE_SUPERADMIN_PASSWORD)) {
    $SuperAdminSecret = ConvertTo-SecureString -String $env:EDUSPHERE_SUPERADMIN_PASSWORD -AsPlainText -Force
}

if ($null -eq $AdminSecret -and -not [string]::IsNullOrWhiteSpace($env:EDUSPHERE_ADMIN_PASSWORD)) {
    $AdminSecret = ConvertTo-SecureString -String $env:EDUSPHERE_ADMIN_PASSWORD -AsPlainText -Force
}

if ($null -eq $AdminSecret) {
    $AdminSecret = ConvertTo-SecureString -String 'EduSphere147' -AsPlainText -Force
}

if ($null -eq $SuperAdminSecret) {
    Write-Error 'Set EDUSPHERE_SUPERADMIN_PASSWORD before running this script.'
    exit 2
}

$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $failures.Add($Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if ($Condition) {
        Write-Host "[PASS] $Message" -ForegroundColor Green
    }
    else {
        Add-Failure $Message
    }
}

function Get-Title {
    param([string]$Html)
    return [regex]::Match($Html, '<title>(.*?)</title>', 'IgnoreCase,Singleline').Groups[1].Value.Trim()
}

function ConvertFrom-SecureStringToPlainText {
    param([securestring]$SecureText)

    if ($null -eq $SecureText) {
        return ''
    }

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureText)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Invoke-ApiLogin {
    param(
        [string]$ApiUrl,
        [string]$Username,
        [securestring]$Secret
    )

    $plainSecret = ConvertFrom-SecureStringToPlainText -SecureText $Secret

    $payload = @{
        username = $Username
        password = $plainSecret
        deviceInfo = 'degree-rules-verifier'
    } | ConvertTo-Json

    return Invoke-RestMethod -Uri "$ApiUrl/api/v1/auth/login" -Method Post -ContentType 'application/json' -Body $payload -TimeoutSec 20
}

function New-WebSessionForUser {
    param(
        [string]$LoginUrl,
        [string]$Username,
        [securestring]$Secret
    )

    $plainSecret = ConvertFrom-SecureStringToPlainText -SecureText $Secret

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri $LoginUrl -WebSession $session -TimeoutSec 20
    $token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value

    if ([string]::IsNullOrWhiteSpace($token)) {
        throw 'Login anti-forgery token was not found.'
    }

    Invoke-WebRequest -Uri $LoginUrl -Method Post -Body @{
        '__RequestVerificationToken' = $token
        username = $Username
        password = $plainSecret
    } -WebSession $session -MaximumRedirection 10 -TimeoutSec 20 | Out-Null

    return $session
}

function Invoke-WebCheck {
    param(
        [string]$WebUrl,
        [string]$Username,
        [securestring]$Secret,
        [bool]$ExpectDegreeRulesPage,
        [bool]$ExpectDegreeRulesMenu
    )

    $session = New-WebSessionForUser -LoginUrl "$WebUrl/Portal/Login" -Username $Username -Secret $Secret

    $dashboard = Invoke-WebRequest -Uri "$WebUrl/Portal/Dashboard" -WebSession $session -TimeoutSec 20
    $degreeRulesInMenu = $dashboard.Content -match 'href="/Portal/DegreeRules"'

    $degreeRules = Invoke-WebRequest -Uri "$WebUrl/Portal/DegreeRules" -WebSession $session -MaximumRedirection 10 -TimeoutSec 20
    $title = Get-Title -Html $degreeRules.Content

    $hasRulesHeading = $degreeRules.Content -match 'Degree Rules'
    $hasCreatePanel = $degreeRules.Content -match 'Create New Degree Rule'
    $hasAccessDeniedMsg = $degreeRules.Content -match 'Access denied for this section based on your current role and menu permissions'
    $hasOnlySuperAdminMsg = $degreeRules.Content -match 'Only SuperAdmin can manage degree rules'
    $hasErrorPrefix = $degreeRules.Content -match 'Error:'

    Write-Host "[INFO] User=$Username; Title=$title; MenuDegreeRules=$degreeRulesInMenu"

    Assert-True ($degreeRulesInMenu -eq $ExpectDegreeRulesMenu) "Menu visibility for $Username matches expectation."

    if ($ExpectDegreeRulesPage) {
        Assert-True ($title -like 'Degree Rules*') "$Username lands on Degree Rules page."
        Assert-True $hasRulesHeading "$Username sees Degree Rules heading."
        Assert-True $hasCreatePanel "$Username sees create panel."
        Assert-True (-not $hasErrorPrefix) "$Username sees no runtime error banner."
        Assert-True (-not $hasAccessDeniedMsg) "$Username is not shown access denied message."
        Assert-True (-not $hasOnlySuperAdminMsg) "$Username is not shown only-superadmin warning."
    }
    else {
        Assert-True ($title -like 'Dashboard*') "$Username is redirected to Dashboard from Degree Rules."
        Assert-True $hasAccessDeniedMsg "$Username sees access denied message."
        Assert-True (-not $hasErrorPrefix) "$Username sees no runtime error banner."
    }
}

function Invoke-ApiRuleAuthCheck {
    param(
        [string]$ApiUrl,
        [string]$Username,
        [securestring]$Secret,
        [int]$ExpectedStatus
    )

    $login = Invoke-ApiLogin -ApiUrl $ApiUrl -Username $Username -Secret $Secret
    $token = $login.accessToken

    try {
        $resp = Invoke-WebRequest -Uri "$ApiUrl/api/v1/degree-audit/rule" -Headers @{ Authorization = "Bearer $token" } -TimeoutSec 20
        $status = [int]$resp.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }

    Assert-True ($status -eq $ExpectedStatus) "API /degree-audit/rule for $Username returns $ExpectedStatus (actual $status)."
}

try {
    $webHealth = Invoke-WebRequest -Uri "$WebBaseUrl/Portal/Login" -TimeoutSec 20
    Assert-True ($webHealth.StatusCode -eq 200) 'Web endpoint is reachable.'

    $apiHealth = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -TimeoutSec 20
    Assert-True ($apiHealth.StatusCode -eq 200) 'API endpoint is reachable.'

    Invoke-WebCheck -WebUrl $WebBaseUrl -Username $SuperAdminUsername -Secret $SuperAdminSecret -ExpectDegreeRulesPage $true -ExpectDegreeRulesMenu $true
    Invoke-WebCheck -WebUrl $WebBaseUrl -Username $AdminUsername -Secret $AdminSecret -ExpectDegreeRulesPage $false -ExpectDegreeRulesMenu $false

    Invoke-ApiRuleAuthCheck -ApiUrl $ApiBaseUrl -Username $SuperAdminUsername -Secret $SuperAdminSecret -ExpectedStatus 200
    Invoke-ApiRuleAuthCheck -ApiUrl $ApiBaseUrl -Username $AdminUsername -Secret $AdminSecret -ExpectedStatus 403
}
catch {
    Add-Failure ("Unhandled verifier error: {0}" -f $_.Exception.Message)
}

if ($failures.Count -gt 0) {
    Write-Host "`nDegree Rules verification failed: $($failures.Count) issue(s)." -ForegroundColor Red
    $failures | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nDegree Rules verification passed." -ForegroundColor Green
exit 0
