param([string]$In, [string]$Terms, [string]$OutDocx, [string]$OutPdf)
$ErrorActionPreference = 'Stop'
$log = Join-Path $PSScriptRoot 'finalize.log'
$sw = [Diagnostics.Stopwatch]::StartNew()
function Step($name) { $l = "{0,6:N1}s  {1}" -f $sw.Elapsed.TotalSeconds, $name; Add-Content -Path $log -Value $l; $l }

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($In, $false, $true, $false)
    Step 'opened'

    # Index entries: one XE field per term, placed at the end of the matching section heading.
    $entries = Get-Content $Terms -Raw | ConvertFrom-Json
    $marked = 0
    # Search body text only: skip the table of contents, which repeats every heading.
    $bodyStart = if ($doc.TablesOfContents.Count -gt 0) { $doc.TablesOfContents.Item(1).Range.End } else { 0 }
    foreach ($e in $entries) {
        $rng = $doc.Range($bodyStart, $doc.Content.End)
        $find = $rng.Find
        $find.ClearFormatting()
        if ($find.Execute($e.heading)) {
            $para = $rng.Paragraphs.Item(1).Range
            foreach ($t in $e.terms) {
                $at = $doc.Range($para.End - 1, $para.End - 1)
                $doc.Fields.Add($at, 4, ('"' + [string]$t + '"'), $false) | Out-Null   # wdFieldIndexEntry (XE)
                $marked++
            }
        }
    }
    $doc.ActiveWindow.View.ShowFieldCodes = $false
    $doc.ActiveWindow.View.ShowHiddenText = $false
    $doc.ActiveWindow.View.ShowAll = $false
    $xe = @($doc.Fields | Where-Object { $_.Type -eq 4 }).Count
    Step "marked $marked index entries; XE fields in document: $xe"

    # Replace the placeholder with a two-column index.
    $rng = $doc.Content
    if ($rng.Find.Execute('INDEX_PLACEHOLDER')) {
        $rng.Text = ''
        $ix = $doc.Indexes.Add($rng, 2, $true, 0, 2)   # letter headings, right-aligned page numbers, indented, 2 columns
        $ix.TabLeader = 1                         # dot leader
    }
    Step 'index inserted'

    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    foreach ($ix in $doc.Indexes) { $ix.Update() }
    Step 'toc and index updated'

    $doc.SaveAs2($OutDocx, 16)
    Step 'saved docx'
    $doc.ExportAsFixedFormat($OutPdf, 17, $false, 0, 0, 0, 0, 0, $true, $true, 1, $true, $true, $false)
    Step ('exported pdf; pages=' + $doc.ComputeStatistics(2))
    $doc.Close($false)
}
finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
