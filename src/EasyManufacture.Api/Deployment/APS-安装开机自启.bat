@echo off
chcp 65001 >nul
cd /d "%~dp0"
title APS 安装开机自启（需管理员）

net session >nul 2>&1
if errorlevel 1 (
  echo.
  echo [提示] 需要管理员权限。正在请求提升...
  echo.
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo 正在注册 APS 为「系统启动时运行」（无需用户登录）...
echo.

REM 勿传 -PublishPath "%~dp0"：路径末尾 \ 会转义引号，导致 F:\APSNEW" 非法路径
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ApsAutoStart.ps1"
set ERR=%ERRORLEVEL%

echo.
if %ERR% NEQ 0 (
  echo [失败] 安装未成功，错误码 %ERR%
) else (
  echo [完成] 重启 Windows 后 APS 将自动启动（无需登录）。
  echo        也可在「任务计划程序」中手动运行 APS 任务测试。
)
echo.
pause
exit /b %ERR%
