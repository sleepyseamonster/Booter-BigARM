param([string]$Blender = 'D:\BooterBigArmTools\Blender522\blender-5.2.2-windows-x64\blender.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$sculptFile = Join-Path $PSScriptRoot 'MinimalSculpt.blend'
$sessionScript = Join-Path $projectRoot 'Tools\Art\Blender\sculpt_session.py'
if (-not (Test-Path -LiteralPath $Blender)) { throw "Blender not found: $Blender" }
# Interactive session starts minimized to preserve the user's foreground app.
$launchArgs = @('--factory-startup', '--disable-autoexec', ('"' + $sculptFile + '"'), '--python', ('"' + $sessionScript + '"'))
Start-Process -FilePath $Blender -ArgumentList $launchArgs -WindowStyle Minimized
