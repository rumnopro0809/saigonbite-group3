param(
    [Parameter(Mandatory = $true)][string]$Docx,
    [Parameter(Mandatory = $true)][string]$Pdf
)
$ErrorActionPreference = 'Stop'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($Docx, $false, $false)
    $doc.Fields.Update() | Out-Null
    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    $doc.Repaginate()
    # OutputFileName, ExportFormat(pdf), OpenAfterExport, OptimizeFor(print),
    # Range(all), From, To, Item(document), IncludeDocProps, KeepIRM,
    # CreateBookmarks(headings), DocStructureTags, BitmapMissingFonts, UseISO19005_1
    $doc.ExportAsFixedFormat($Pdf, 17, $false, 0, 0, 0, 0, 0, $true, $true, 1, $true, $true, $false)
    $doc.Close(-1)
}
finally {
    $word.Quit()
}
