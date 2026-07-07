@echo off
chcp 65001 >nul
cd /d "%~dp0"
title APS 结束旧进程

echo 仅结束【本目录 appsettings 配置端口】上的 APS 实例，不影响其他端口。
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-ApsListenPort.ps1" -KillApsOnly
set ERR=%ERRORLEVEL%

echo.
if %ERR% EQU 0 (
  echo 端口可用，可以重新运行 APS-启动.bat
) else (
  echo 端口仍被占用，请查看上方 PID；勿使用 taskkill /IM APS.exe /F（会误杀其他实例）
  echo 可手动: netstat -ano ^| findstr LISTENING
)
echo.
pause
exit /b %ERR%
