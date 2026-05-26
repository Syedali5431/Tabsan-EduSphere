param(
    [Parameter(Mandatory = $false)]
    [string]$ResultsDirectory = "TestResults",

    [Parameter(Mandatory = $false)]
    [string]$OutputMarkdown = "test-module-summary.md",

    [Parameter(Mandatory = $false)]
    [string]$OutputJson = "test-module-summary.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$modulePatterns = @(
    @{ Name = "Security"; Pattern = "(?i)security|password|mfa|twofactor|credential|auth" },
    @{ Name = "Authorization"; Pattern = "(?i)authoriz|role|permission|policy|access" },
    @{ Name = "SidebarAndMenus"; Pattern = "(?i)sidebar|menu" },
    @{ Name = "Reporting"; Pattern = "(?i)report|transcript|degree|export" },
    @{ Name = "Analytics"; Pattern = "(?i)analytics|dashboard" },
    @{ Name = "StudentLifecycle"; Pattern = "(?i)studentlifecycle|enrollment|graduation|waitlist" },
    @{ Name = "ImportAndOnboarding"; Pattern = "(?i)import|onboard|registration|whitelist" },
    @{ Name = "InfrastructureAndRuntime"; Pattern = "(?i)infrastructure|webfactory|phase36|health|license|performance|observability" }
)

function Resolve-ModuleName {
    param([string]$Identity)

    foreach ($entry in $modulePatterns) {
        if ($Identity -match $entry.Pattern) {
            return $entry.Name
        }
    }

    return "Uncategorized"
}

if (-not (Test-Path -Path $ResultsDirectory)) {
    "# Test Module Summary`n`nNo results directory found at '$ResultsDirectory'." | Set-Content -Path $OutputMarkdown -Encoding UTF8
    @{ generatedUtc = (Get-Date).ToUniversalTime().ToString("o"); note = "No results directory found." } |
        ConvertTo-Json -Depth 5 |
        Set-Content -Path $OutputJson -Encoding UTF8
    Write-Host "No results directory found; summary stubs generated."
    exit 0
}

$trxFiles = Get-ChildItem -Path $ResultsDirectory -Recurse -Filter *.trx -File
if ($trxFiles.Count -eq 0) {
    "# Test Module Summary`n`nNo TRX files found under '$ResultsDirectory'." | Set-Content -Path $OutputMarkdown -Encoding UTF8
    @{ generatedUtc = (Get-Date).ToUniversalTime().ToString("o"); note = "No TRX files found." } |
        ConvertTo-Json -Depth 5 |
        Set-Content -Path $OutputJson -Encoding UTF8
    Write-Host "No TRX files found; summary stubs generated."
    exit 0
}

$rows = New-Object System.Collections.Generic.List[object]

foreach ($trx in $trxFiles) {
    [xml]$xml = Get-Content -Path $trx.FullName

    $testIdMap = @{}
    $unitTests = Select-Xml -Xml $xml -XPath "//*[local-name()='UnitTest']"
    foreach ($ut in $unitTests) {
        $node = $ut.Node
        $testId = [string]$node.id
        if ([string]::IsNullOrWhiteSpace($testId)) {
            continue
        }

        $method = $node.SelectSingleNode("*[local-name()='TestMethod']")
        $className = if ($null -ne $method) { [string]$method.className } else { "" }
        $methodName = if ($null -ne $method) { [string]$method.name } else { [string]$node.name }

        $fullName = if (-not [string]::IsNullOrWhiteSpace($className)) {
            "$className.$methodName"
        }
        else {
            [string]$node.name
        }

        $testIdMap[$testId] = $fullName
    }

    $results = Select-Xml -Xml $xml -XPath "//*[local-name()='UnitTestResult']"
    foreach ($result in $results) {
        $node = $result.Node
        $testId = [string]$node.testId
        $testName = [string]$node.testName
        $outcome = [string]$node.outcome
        $identity = if ($testIdMap.ContainsKey($testId)) { $testIdMap[$testId] } else { $testName }
        $module = Resolve-ModuleName -Identity $identity

        $rows.Add([pscustomobject]@{
                module   = $module
                outcome  = $outcome
                identity = $identity
                file     = $trx.FullName
            }) | Out-Null
    }
}

$summary = $rows |
    Group-Object -Property module |
    ForEach-Object {
        $moduleRows = $_.Group
        $total = $moduleRows.Count
        $passed = ($moduleRows | Where-Object { $_.outcome -eq "Passed" }).Count
        $failed = ($moduleRows | Where-Object { $_.outcome -eq "Failed" }).Count
        $skipped = ($moduleRows | Where-Object { $_.outcome -eq "NotExecuted" -or $_.outcome -eq "Skipped" }).Count

        [pscustomobject]@{
            Module = $_.Name
            Total = $total
            Passed = $passed
            Failed = $failed
            Skipped = $skipped
            PassRate = if ($total -eq 0) { 0 } else { [math]::Round(($passed / $total) * 100, 2) }
        }
    } |
    Sort-Object -Property @{Expression = "Failed"; Descending = $true }, @{Expression = "Total"; Descending = $true }, @{Expression = "Module"; Descending = $false }

$totals = [pscustomobject]@{
    Total = $rows.Count
    Passed = ($rows | Where-Object { $_.outcome -eq "Passed" }).Count
    Failed = ($rows | Where-Object { $_.outcome -eq "Failed" }).Count
    Skipped = ($rows | Where-Object { $_.outcome -eq "NotExecuted" -or $_.outcome -eq "Skipped" }).Count
}

$failedTests = $rows |
    Where-Object { $_.outcome -eq "Failed" } |
    Select-Object -Property module, identity, file

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# Test Module Summary") | Out-Null
$md.Add("") | Out-Null
$md.Add("Generated UTC: $((Get-Date).ToUniversalTime().ToString('u'))") | Out-Null
$md.Add("") | Out-Null
$md.Add("## Overall") | Out-Null
$md.Add("") | Out-Null
$md.Add("- Total: $($totals.Total)") | Out-Null
$md.Add("- Passed: $($totals.Passed)") | Out-Null
$md.Add("- Failed: $($totals.Failed)") | Out-Null
$md.Add("- Skipped: $($totals.Skipped)") | Out-Null
$md.Add("") | Out-Null
$md.Add("## Module Health") | Out-Null
$md.Add("") | Out-Null
$md.Add("| Module | Total | Passed | Failed | Skipped | PassRate (%) |") | Out-Null
$md.Add("| --- | ---: | ---: | ---: | ---: | ---: |") | Out-Null

foreach ($item in $summary) {
    $md.Add("| $($item.Module) | $($item.Total) | $($item.Passed) | $($item.Failed) | $($item.Skipped) | $($item.PassRate) |") | Out-Null
}

$md.Add("") | Out-Null
$md.Add("## Failed Tests") | Out-Null
$md.Add("") | Out-Null

if ($failedTests.Count -eq 0) {
    $md.Add("No failed tests.") | Out-Null
}
else {
    $md.Add("| Module | Test | Source TRX |") | Out-Null
    $md.Add("| --- | --- | --- |") | Out-Null
    foreach ($failed in $failedTests) {
        $safeIdentity = $failed.identity -replace "\|", "\\|"
        $safeFile = $failed.file -replace "\|", "\\|"
        $md.Add("| $($failed.module) | $safeIdentity | $safeFile |") | Out-Null
    }
}

$md | Set-Content -Path $OutputMarkdown -Encoding UTF8

@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    totals = $totals
    modules = $summary
    failedTests = $failedTests
} |
    ConvertTo-Json -Depth 8 |
    Set-Content -Path $OutputJson -Encoding UTF8

Write-Host "Generated module health summary: $OutputMarkdown"
Write-Host "Generated module health JSON: $OutputJson"
