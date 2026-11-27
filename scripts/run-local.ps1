# Builds SaigonBite with MSBuild and runs the web app on IIS Express for local testing.
# The first request creates the SaigonBiteDb database on .\SQLEXPRESS, applies migrations and seeds sample data.
# Kept ASCII-only so Windows PowerShell 5 reads it correctly without a BOM.
param(
    [int]$Port = 5099,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$web = Join-Path $root 'src\SaigonBite.Web'

if (-not $NoBuild) {
    # Classic ASP.NET MVC 5 projects need Visual Studio's MSBuild (dotnet build cannot load WebApplication.targets).
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Install Visual Studio 2022 or Build Tools 2022 with the web workload.' }
    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild was not found. Install the ASP.NET and web development workload.' }
    & $msbuild (Join-Path $root 'SaigonBite.sln') /restore /t:Build /p:Configuration=Debug /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

$candidates = @(
    (Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'IIS Express\iisexpress.exe')
)
$iis = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iis) {
    throw 'IIS Express not found. It ships with Visual Studio, or install it from https://www.microsoft.com/download/details.aspx?id=48264'
}

Write-Host "SaigonBite    : http://localhost:$Port/"
Write-Host "Sign in       : http://localhost:$Port/Account/Login  (sample accounts in README, password Saigon123!)"
Write-Host 'Press Ctrl+C to stop.'
& $iis "/path:$web" "/port:$Port" '/systray:false'
