# Runs the SaigonBite web app locally for manual testing (no Visual Studio needed).
# Uses the `SaigonBite` database on SQL Server Express (connection string in src\SaigonBite.Web\Web.config).
# The first request creates the database, applies migrations and loads sample data.
# Kept ASCII-only so Windows PowerShell 5 reads it correctly without a BOM.
param(
    [int]$Port = 5099,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$web = Join-Path $root 'src\SaigonBite.Web'

if (-not $NoBuild) {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    $dotnetPath = if ($dotnet) { $dotnet.Source } else { Join-Path $env:ProgramFiles 'dotnet\dotnet.exe' }
    if (-not (Test-Path -LiteralPath $dotnetPath)) { throw 'Install the .NET SDK to build: https://dotnet.microsoft.com/download' }
    & $dotnetPath build (Join-Path $root 'SaigonBite.sln') --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

$candidates = @(
    (Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'IIS Express\iisexpress.exe'),
    (Join-Path $root 'tmp\aspnet-host\iis\IIS Express\iisexpress.exe')
)
$iis = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iis) {
    throw 'IIS Express not found. Install the free version from https://www.microsoft.com/download/details.aspx?id=48264 and run again.'
}

Write-Host "Customer site : http://localhost:$Port/"
Write-Host "Kitchen       : http://localhost:$Port/Kitchen/Login"
Write-Host "Shipper       : http://localhost:$Port/Shipper/Login"
Write-Host 'Press Ctrl+C to stop.'
& $iis "/path:$web" "/port:$Port" '/systray:false'
