<#
.SYNOPSIS
  Plan M12 — builds the EduSphere app for one institution (white-label, premium licence).

.DESCRIPTION
  Produces a store-ready Android App Bundle (.aab, or an .apk with -Apk) or an iOS archive (.ipa, on a Mac) with the
  institution's name, application id, API address, colour, sign-in URL scheme and version. Output goes to
  mobile/artifacts/<AppId>/<Version>-<Build>/.

  The API of that institution must use the same scheme: Mobile:AppRedirectUri = "<Scheme>://sso" (appsettings.json).

.EXAMPLE
  ./build-whitelabel.ps1 -AppName "City University" -AppId edu.cityuni.app -ApiBaseUrl https://api.cityuni.edu `
      -Color "#7A1F2B" -Scheme cityuni -Version 1.2.0 -Build 12 -GoogleServicesJson ./cityuni/google-services.json `
      -KeyStore ./cityuni/upload.keystore -KeyAlias upload
  (the keystore passwords are read from the environment: EDUSPHERE_KEYSTORE_PASS and EDUSPHERE_KEY_PASS)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $AppName,
    [Parameter(Mandatory)] [ValidatePattern('^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$')] [string] $AppId,
    [Parameter(Mandatory)] [ValidatePattern('^https://')] [string] $ApiBaseUrl,
    [ValidatePattern('^#[0-9A-Fa-f]{6}$')] [string] $Color = '#0F2A4A',
    [ValidatePattern('^[a-z][a-z0-9.+-]{1,31}$')] [string] $Scheme = 'edusphere',
    [Parameter(Mandatory)] [ValidatePattern('^\d+\.\d+\.\d+$')] [string] $Version,
    [Parameter(Mandatory)] [ValidateRange(1, 2100000000)] [int] $Build,
    [ValidateSet('android', 'ios')] [string] $Platform = 'android',
    [string] $GoogleServicesJson,
    [string] $GoogleServiceInfoPlist,
    [string] $KeyStore,
    [string] $KeyAlias,
    [switch] $Apk
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj'
$out = Join-Path $PSScriptRoot "artifacts/$AppId/$Version-$Build"
New-Item -ItemType Directory -Force -Path $out | Out-Null

$props = @(
    "-p:AppName=$AppName", "-p:AppId=$AppId", "-p:ApiBaseUrl=$ApiBaseUrl", "-p:AppColor=$Color",
    "-p:AppScheme=$Scheme", "-p:AppVersion=$Version", "-p:AppBuild=$Build"
)

if ($Platform -eq 'android') {
    $framework = 'net9.0-android'
    if ($GoogleServicesJson) {
        if (-not (Test-Path $GoogleServicesJson)) { throw "google-services.json not found: $GoogleServicesJson" }
        $json = Get-Content $GoogleServicesJson -Raw | ConvertFrom-Json
        $packages = @($json.client | ForEach-Object { $_.client_info.android_client_info.package_name })
        if ($packages -notcontains $AppId) { throw "google-services.json is for $($packages -join ', '), not $AppId. Download the file for this app id from Firebase." }
        $props += "-p:GoogleServicesJsonPath=$((Resolve-Path $GoogleServicesJson).Path)"
    } else {
        Write-Warning 'No google-services.json: the app is built without push notifications.'
        $props += '-p:UseFirebase=false'
    }
    $props += "-p:AndroidPackageFormat=$(if ($Apk) { 'apk' } else { 'aab' })"
    if ($KeyStore) {
        if (-not $env:EDUSPHERE_KEYSTORE_PASS) { throw 'Set EDUSPHERE_KEYSTORE_PASS (and EDUSPHERE_KEY_PASS) for the keystore.' }
        $props += '-p:AndroidKeyStore=true', "-p:AndroidSigningKeyStore=$((Resolve-Path $KeyStore).Path)", "-p:AndroidSigningKeyAlias=$KeyAlias",
            '-p:AndroidSigningStorePass=env:EDUSPHERE_KEYSTORE_PASS', "-p:AndroidSigningKeyPass=env:$(if ($env:EDUSPHERE_KEY_PASS) { 'EDUSPHERE_KEY_PASS' } else { 'EDUSPHERE_KEYSTORE_PASS' })"
    } else {
        Write-Warning 'No keystore: the package is signed with the debug key and cannot be uploaded to Google Play.'
    }
} else {
    if (-not $IsMacOS) { throw 'iOS builds need a Mac with Xcode (and an Apple developer account for signing).' }
    $framework = 'net9.0-ios'
    # The sign-in return scheme lives in Info.plist on iOS.
    $plist = Join-Path $PSScriptRoot 'Tabsan.EduSphere.Mobile/Platforms/iOS/Info.plist'
    $text = Get-Content $plist -Raw
    $text = [regex]::Replace($text, '(<key>CFBundleURLSchemes</key>\s*<array>\s*<string>)[^<]*(</string>)', "`${1}$Scheme`${2}")
    Set-Content -Path $plist -Value $text -NoNewline
    if ($GoogleServiceInfoPlist) {
        Copy-Item $GoogleServiceInfoPlist (Join-Path $PSScriptRoot 'Tabsan.EduSphere.Mobile/Platforms/iOS/GoogleService-Info.plist') -Force
    }
    $props += '-p:ArchiveOnBuild=true'
}

Push-Location $PSScriptRoot
try {
    dotnet publish $project -c Release -f $framework "-p:MobileTargets=$framework" @props -o $out
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }
} finally { Pop-Location }

Get-ChildItem $out -Include *.aab, *.apk, *.ipa -Recurse | ForEach-Object { "Built $($_.FullName) ($([math]::Round($_.Length / 1MB, 1)) MB)" }
"Remember: the institution's API needs Mobile:AppRedirectUri = `"$($Scheme)://sso`" and Mobile:LatestVersion = `"$Version`"."
