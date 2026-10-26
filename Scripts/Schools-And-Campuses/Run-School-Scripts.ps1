# Runs the Schools-And-Campuses scripts in order (01 → 02 → 03) with sqlcmd.
# Usage:  .\Run-School-Scripts.ps1 [-Server "(localdb)\MSSQLLocalDB"] [-Database "Tabsan-EduSphere"] [-CheckOnly]
param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "Tabsan-EduSphere",
    [switch]$CheckOnly
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$scripts = if ($CheckOnly) { @("03-Check-Institution-Types.sql") } else { @("01-Add-Schools-And-Campuses.sql", "02-Add-School-Sections.sql", "03-Check-Institution-Types.sql") }

foreach ($script in $scripts) {
    Write-Host "== $script" -ForegroundColor Cyan
    # -b stops on SQL errors; -f 65001 reads the files as UTF-8.
    & sqlcmd -S $Server -d $Database -b -f 65001 -W -i (Join-Path $here $script)
    if ($LASTEXITCODE -ne 0) { throw "$script failed (sqlcmd exit code $LASTEXITCODE)." }
}
