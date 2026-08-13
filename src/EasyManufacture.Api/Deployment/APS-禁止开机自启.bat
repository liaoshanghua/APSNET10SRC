@echo off
chcp 65001 >nul
cd /d "%~dp0"

net session >nul 2>&1
if errorlevel 1 (
  echo 请右键本文件 -^> 以管理员身份运行
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-ApsAutoStart.ps1" -PublishPath "%~dp0"
if errorlevel 1 (
  echo.
  pause
  exit /b 1
)
echo.
pause
