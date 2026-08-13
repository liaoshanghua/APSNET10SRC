@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul
cd /d "%~dp0"
title APS 服务
if not exist logs mkdir logs

echo.
echo ============================================
echo   这是 APS 系统的启动程序，请不要关闭
echo ============================================
echo   关闭本窗口或点「退出」均会弹出确认框
echo   日志目录: %~dp0logs
echo ============================================
echo.

echo [%date% %time%] APS-启动.bat >> logs\startup.log

if exist "%~dp0Install-ApsDependencies.ps1" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" >> logs\deps-install.log 2>&1
  if errorlevel 1 (
    echo.
    echo [警告] 依赖检查未通过，详见 logs\deps-install.log
    echo.
  )
)

if exist "%~dp0\.dotnet-local-path" (
  set /p _DOTNET_DIR=<"%~dp0\.dotnet-local-path"
  set DOTNET_ROOT=!_DOTNET_DIR!
  set PATH=!_DOTNET_DIR!;!PATH!
)

call :CheckRuntime
if !ERRORLEVEL! equ 0 goto :RuntimeOk

call :ShowRuntimeError !ERRORLEVEL!
call :TryOfferRuntimeInstall
call :CheckRuntime
if !ERRORLEVEL! neq 0 (
  pause
  exit /b 1
)

:RuntimeOk
call :CheckPortFree
if !ERRORLEVEL! neq 0 (
  echo.
  echo 是否尝试结束 APS.exe 后重新检测？ [Y/N]
  choice /C YN /N /M "请选择 Y 或 N: "
  if errorlevel 2 goto :PortBlocked
  if errorlevel 1 (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-ApsListenPort.ps1" -KillApsOnly
    call :CheckPortFree
    if !ERRORLEVEL! neq 0 goto :PortBlocked
  )
)
if not exist "%~dp0APS.exe" (
  if not exist "%~dp0APS.dll" (
    echo [错误] 未找到 APS.exe / APS.dll，请重新 publish。
    pause
    exit /b 1
  )
)

set ASPNETCORE_ENVIRONMENT=Production
echo [%date% %time%] starting APS.exe >> logs\startup.log
echo 正在启动 APS，请稍候...
echo.

if exist "%~dp0APS.exe" (
  "%~dp0APS.exe"
) else (
  dotnet "%~dp0APS.dll"
)
set EXITCODE=!ERRORLEVEL!
echo [%date% %time%] APS exited !EXITCODE! >> logs\startup.log

if !EXITCODE! NEQ 0 (
  echo.
  echo [错误] APS 异常退出，代码: !EXITCODE!
  echo.
  if exist logs\aps-crash.log (
    echo ===== logs\aps-crash.log =====
    type logs\aps-crash.log
    echo.
  )
  if exist logs\aps-console.log (
    echo ===== logs\aps-console.log 末尾 =====
    powershell -NoProfile -Command "Get-Content -Path 'logs\aps-console.log' -Tail 40 -ErrorAction SilentlyContinue"
    echo.
  )
  echo 常见原因：配置端口被占用 / 数据库连不上 / 缺少 aspnetcore-runtime 或 windowsdesktop-runtime
  pause
  exit /b !EXITCODE!
)

echo APS 已正常停止。
pause
exit /b 0

:CheckPortFree
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-ApsListenPort.ps1"
set _RC=%ERRORLEVEL%
exit /b %_RC%

:PortBlocked
echo.
echo 请先运行 APS-结束旧进程.bat，或修改 appsettings.json 中的端口。
pause
exit /b 1

:CheckRuntime
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-ApsDotNetRuntime.ps1"
set _RC=%ERRORLEVEL%
exit /b %_RC%

:ShowRuntimeError
set _RC=%~1
echo.
if "%_RC%"=="10" (
  echo [错误] 未找到 dotnet 命令，且系统未安装 .NET 10 运行时。
) else if "%_RC%"=="2" (
  echo [错误] 未检测到 ASP.NET Core 10 运行时！
) else if "%_RC%"=="3" (
  echo [错误] 未检测到 Windows Desktop 10 运行时！
) else (
  echo [错误] 未检测到完整的 .NET 10 运行时（ASP.NET Core + Windows Desktop）！
)
echo.
echo   runtime 目录需要三个安装包（缺一不可）：
echo     dotnet-runtime-*-win-x64.exe
echo     aspnetcore-runtime-*-win-x64.exe
echo     windowsdesktop-runtime-*-win-x64.exe
echo   注意：dotnet-sdk-*.exe 是 SDK，不能替代上述 runtime！
echo.
echo   处理：右键「APS-安装运行时.bat」-^> 以管理员身份运行，然后重新运行本脚本。
echo   或手动以管理员身份运行 runtime 目录下的三个 exe。
echo.
exit /b 0

:TryOfferRuntimeInstall
set _HAS_PACK=0
if exist "%~dp0runtime\" (
  dir /b "%~dp0runtime\aspnetcore-runtime-*-win-x64.exe" >nul 2>&1 && set _HAS_PACK=1
)
if not "!_HAS_PACK!"=="1" exit /b 0

echo runtime 目录已有安装包，是否现在以管理员身份安装？ [Y/N]
choice /C YN /N /M "请选择 Y 或 N: "
if errorlevel 2 exit /b 0
if errorlevel 1 (
  echo 正在请求管理员权限安装...
  powershell -NoProfile -Command "Start-Process -FilePath '%~dp0APS-安装运行时.bat' -Verb RunAs -Wait"
)
exit /b 0
