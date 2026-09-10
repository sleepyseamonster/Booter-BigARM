@echo off
setlocal
set "package_dir=%~dp0"
if not exist "%package_dir%UserData" mkdir "%package_dir%UserData"
if not exist "%package_dir%UserData\rock.json" copy /b "%package_dir%bin\Assets\Recipes\rock.json" "%package_dir%UserData\rock.json" >nul
if errorlevel 1 exit /b 1
set "inspection_args="
if exist "%package_dir%UserData\inspection.json" set inspection_args=--inspection "%package_dir%UserData\inspection.json"
set "model_args="
if exist "%package_dir%bin\Assets\Models\Calibration\model.json" set model_args=--model "%package_dir%bin\Assets\Models\Calibration\model.json"
cd /d "%package_dir%UserData"
"%package_dir%bin\engine_workbench.exe" --rock "%package_dir%UserData\rock.json" --save-inspection "%package_dir%UserData\inspection.json" %inspection_args% %model_args% %*
exit /b %errorlevel%
