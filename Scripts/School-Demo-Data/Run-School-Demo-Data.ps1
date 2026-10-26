# Fills the demo schools with data: 01-School-Records.sql with sqlcmd, then 02-Learning-Activity.mjs through the API.
# Usage:  .\Run-School-Demo-Data.ps1 [-Server "(localdb)\MSSQLLocalDB"] [-Database "Tabsan-EduSphere"] [-Api "http://localhost:5181/api/v1"]
#                                    [-RecordsOnly | -ActivityOnly]
# The API must be running for 02 (it is skipped with -RecordsOnly).
param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "Tabsan-EduSphere",
    [string]$Api = "http://localhost:5181/api/v1",
    [switch]$RecordsOnly,
    [switch]$ActivityOnly
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $ActivityOnly) {
    Write-Host "== 01-School-Records.sql" -ForegroundColor Cyan
    # -b stops on SQL errors; -f 65001 reads the file as UTF-8.
    & sqlcmd -S $Server -d $Database -b -f 65001 -W -i (Join-Path $here "01-School-Records.sql")
    if ($LASTEXITCODE -ne 0) { throw "01-School-Records.sql failed (sqlcmd exit code $LASTEXITCODE)." }
}

if (-not $RecordsOnly) {
    Write-Host "== 02-Learning-Activity.mjs ($Api)" -ForegroundColor Cyan
    try { Invoke-WebRequest -Uri (($Api -replace '/api/v1/?$', '') + "/health") -UseBasicParsing -TimeoutSec 10 | Out-Null }
    catch { throw "The API is not answering at $Api. Start it, then run: .\Run-School-Demo-Data.ps1 -ActivityOnly" }
    & node (Join-Path $here "02-Learning-Activity.mjs") $Api
    if ($LASTEXITCODE -ne 0) { throw "02-Learning-Activity.mjs reported problems (exit code $LASTEXITCODE); it is safe to run again." }
}
