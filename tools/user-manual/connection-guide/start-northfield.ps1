# Starts the guide's example installation "Northfield Model School": a published API and portal with their own database,
# on other ports than the main installation, so both run on one PC.
#   .\start-northfield.ps1 -Part api    -Root <publish folder>   (API on :5281, database EduSphere-Northfield)
#   .\start-northfield.ps1 -Part portal -Root <publish folder>   (portal on :5163, talks to the API on :5281)
# <publish folder> holds api\ and portal\ from:
#   dotnet publish src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -c Release -o <publish folder>\api
#   dotnet publish src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -c Release -o <publish folder>\portal
param(
    [Parameter(Mandatory)][ValidateSet('api', 'portal')][string]$Part,
    [Parameter(Mandatory)][string]$Root,
    [string]$Database = 'EduSphere-Northfield',
    [string]$SqlServer = 'localhost\SQLEXPRESS',
    [int]$ApiPort = 5281,
    [int]$PortalPort = 5163,
    # The address phones use to open the portal (quizzes, some materials, single sign-on).
    [string]$PortalAddressForPhones = 'http://192.168.1.109:5163'
)
$ErrorActionPreference = 'Stop'

# Development: the example runs on one PC without Redis and real secrets (a live server uses Production, guide chapter 8).
$env:ASPNETCORE_ENVIRONMENT = 'Development'
# Environment variables win over every settings file, so this installation can never pick up the main database.
$env:EDUSPHERE_DB_CONNECTION = "Server=$SqlServer;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

if ($Part -eq 'api') {
    # First start on an empty database creates the SuperAdmin; demo password of this example only.
    if (-not $env:TABSAN_SUPER_PASSWORD) { $env:TABSAN_SUPER_PASSWORD = 'Northfield#Admin2026' }
    Set-Location (Join-Path $Root 'api')
    & .\Tabsan.EduSphere.API.exe --urls "http://0.0.0.0:$ApiPort" "--Mobile:PortalBaseUrl=$PortalAddressForPhones" `
        --Licensing:EnforceReadOnlyWhenUnlicensed=false
}
else {
    # The portal finds its API here (this also wins over EduApi:BaseUrl in the settings files).
    $env:EDUSPHERE_APP_BASE_URL = "http://localhost:$ApiPort"
    Set-Location (Join-Path $Root 'portal')
    & .\Tabsan.EduSphere.Web.exe --urls "http://0.0.0.0:$PortalPort"
}
