@echo off

cd /d "%~dp0"

if not exist logs mkdir logs



if exist "%~dp0Install-ApsDependencies.ps1" (

  powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" >> logs\deps-install.log 2>&1

)



if exist "%~dp0.dotnet-local-path" (

  set /p _DOTNET_DIR=<"%~dp0.dotnet-local-path"

  set DOTNET_ROOT=%_DOTNET_DIR%

  set PATH=%_DOTNET_DIR%;%PATH%

)



set ASPNETCORE_ENVIRONMENT=Production
echo [%date% %time%] start-api.bat >> logs\startup.log

if exist "%~dp0APS.exe" (

  "%~dp0APS.exe" >> "%~dp0logs\aps-console.log" 2>&1

) else (

  dotnet "%~dp0APS.dll" >> "%~dp0logs\aps-console.log" 2>&1

)

echo [%date% %time%] exited !ERRORLEVEL! >> "%~dp0logs\startup.log"

