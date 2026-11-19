param(
    [Parameter(Mandatory = $true)][string]$Pptx,
    [Parameter(Mandatory = $true)][string]$Pdf
)
$ErrorActionPreference = 'Stop'
$ppt = New-Object -ComObject PowerPoint.Application
try {
    $pres = $ppt.Presentations.Open($Pptx, $true, $false, $false)
    $pres.SaveAs($Pdf, 32)  # ppSaveAsPDF
    $pres.Close()
}
finally {
    $ppt.Quit()
}
