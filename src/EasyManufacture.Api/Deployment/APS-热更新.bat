@echo off

setlocal EnableDelayedExpansion

chcp 65001 >nul

cd /d "%~dp0"

title APS 热更新



echo.

echo ============================================

echo   APS 热更新（停服 -^> 覆盖 -^> 启动）

echo ============================================

echo   安装目录: %~dp0

echo   更新来源: %~dp0update\

echo   保留: appsettings.json、register.ini、logs

echo ============================================

echo.



if not exist "%~dp0update\" (

  echo [错误] 未找到 update 目录。

  echo 请先把 publish 发布包复制到: %~dp0update\

  echo.

  pause

  exit /b 1

)



if not exist "%~dp0Apply-ApsHotUpdate.ps1" (

  echo [错误] 未找到 Apply-ApsHotUpdate.ps1

  pause

  exit /b 1

)



powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Apply-ApsHotUpdate.ps1" -InstallDir "%~dp0." %*

set ERR=!ERRORLEVEL!



echo.

if !ERR! EQU 0 (

  echo 热更新流程已结束。

) else (

  echo [错误] 热更新失败，退出码 !ERR!，详见 logs\hot-update.log

)

echo.

pause

exit /b !ERR!

