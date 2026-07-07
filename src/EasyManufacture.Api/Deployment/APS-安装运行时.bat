@echo off
chcp 65001 >nul
cd /d "%~dp0"
title APS 安装 .NET 10 运行时（需管理员）

net session >nul 2>&1
if errorlevel 1 (
  echo.
  echo [提示] 安装 .NET 运行时需要管理员权限，正在请求提升...
  echo.
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo 正在安装 runtime 目录中的 .NET 10 运行时...
echo 日志: %~dp0logs\deps-install.log
echo.

if not exist logs mkdir logs
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" -Force
set ERR=%ERRORLEVEL%

echo.
if %ERR% NEQ 0 (
  echo [失败] 安装未成功，错误码 %ERR%，详见 logs\deps-install.log
) else (
  echo [完成] 运行时安装成功，请重新运行 APS-启动.bat
)
echo.
pause
exit /b %ERR%
