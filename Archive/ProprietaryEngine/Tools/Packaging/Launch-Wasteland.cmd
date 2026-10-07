@echo off
setlocal
set "terrain_package=%~dp0"
if not exist "%terrain_package%UserData" mkdir "%terrain_package%UserData"
set "terrain_inspection=%terrain_package%bin\Assets\inspection.json"
if exist "%terrain_package%UserData\inspection.json" set "terrain_inspection=%terrain_package%UserData\inspection.json"
cd /d "%terrain_package%UserData"
"%terrain_package%bin\engine_workbench.exe" --terrain "%terrain_package%bin\Assets\Recipes\terrain.json" --stream-rock "%terrain_package%bin\Assets\Recipes\stream-rock.json" --terrain-preview --world-profile "%terrain_package%UserData\World" --inspection "%terrain_inspection%" --save-inspection "%terrain_package%UserData\inspection.json" %*
