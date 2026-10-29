param([string]$Compiler = 'D:\Visual Studio\MSBuild\Current\Bin\Roslyn\csc.exe')
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Supply a C# compiler using -Compiler.' }
New-Item -ItemType Directory -Path 'src\SaigonBite.Domain\bin','tests\SaigonBite.Domain.Checks\bin' -Force | Out-Null
& $Compiler /nologo /target:library /out:src\SaigonBite.Domain\bin\SaigonBite.Domain.dll src\SaigonBite.Domain\Pricing.cs src\SaigonBite.Domain\OrderLifecycle.cs
if ($LASTEXITCODE -ne 0) { throw 'Domain compile failed.' }
& $Compiler /nologo /r:src\SaigonBite.Domain\bin\SaigonBite.Domain.dll /out:tests\SaigonBite.Domain.Checks\bin\SaigonBite.Domain.Checks.exe tests\SaigonBite.Domain.Checks\Program.cs
if ($LASTEXITCODE -ne 0) { throw 'Check runner compile failed.' }
Copy-Item -LiteralPath 'src\SaigonBite.Domain\bin\SaigonBite.Domain.dll' -Destination 'tests\SaigonBite.Domain.Checks\bin\SaigonBite.Domain.dll' -Force
& '.\tests\SaigonBite.Domain.Checks\bin\SaigonBite.Domain.Checks.exe'
if ($LASTEXITCODE -ne 0) { throw 'Domain checks failed.' }
