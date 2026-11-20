param([string]$PlantUmlJar = '.tools\plantuml.jar')
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $PlantUmlJar)) { throw 'Supply a local PlantUML jar with -PlantUmlJar. See docs/part2/diagrams/README.md.' }
$dotArgs = @()
$dotPath = 'C:\Program Files\Graphviz\bin\dot.exe'
if (Test-Path -LiteralPath $dotPath) {
    $env:GRAPHVIZ_DOT = $dotPath
    $dotArgs = @('-graphvizdot', $dotPath)
}
& java '-Djava.awt.headless=true' -jar $PlantUmlJar @dotArgs -charset UTF-8 -failfast2 -tsvg 'docs/part2/diagrams/*.puml'
if ($LASTEXITCODE -ne 0) { throw 'PlantUML SVG rendering failed.' }
& java '-Djava.awt.headless=true' -jar $PlantUmlJar @dotArgs -charset UTF-8 -failfast2 -tpng 'docs/part2/diagrams/*.puml'
if ($LASTEXITCODE -ne 0) { throw 'PlantUML PNG rendering failed.' }
