param([string]$Blender = 'D:\BooterBigArmTools\Blender522\blender-5.2.2-windows-x64\blender.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$source = Join-Path $PSScriptRoot 'RockSculptStarter_2026-10-09.blend'
$session = Join-Path $projectRoot 'Tools\Art\Blender\sculpt_session.py'
if (-not (Test-Path -LiteralPath $Blender)) { throw "Blender not found: $Blender" }
if (-not (Test-Path -LiteralPath $source)) { throw "Local Blender source not found: $source" }
# Interactive authoring window is requested; Blender's supported flag preserves foreground focus.
# The file already saves Sculpting workspace/mode. This helper changes navigation preferences only.
$launchArgs = @('--factory-startup', '--disable-autoexec', '--no-window-focus', '--window-border',
    ('"' + $source + '"'), '--python', ('"' + $session + '"'))
Start-Process -FilePath $Blender -ArgumentList $launchArgs -WindowStyle Normal
