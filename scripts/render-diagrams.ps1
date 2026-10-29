param([string]$PlantUmlJar = '.tools\plantuml.jar')
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $PlantUmlJar)) { throw 'Supply a local PlantUML jar with -PlantUmlJar. See docs/part2/diagrams/README.md.' }
& java '-Djava.awt.headless=true' -jar $PlantUmlJar -charset UTF-8 -failfast2 -tsvg 'docs/part2/diagrams/*.puml'
if ($LASTEXITCODE -ne 0) { throw 'PlantUML SVG rendering failed.' }
& java '-Djava.awt.headless=true' -jar $PlantUmlJar -charset UTF-8 -failfast2 -tpng 'docs/part2/diagrams/*.puml'
if ($LASTEXITCODE -ne 0) { throw 'PlantUML PNG rendering failed.' }
