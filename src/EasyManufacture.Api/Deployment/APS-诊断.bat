@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul
cd /d "%~dp0"
title APS 诊断
echo ===== APS 环境诊断 =====
echo 目录: %~dp0
echo.

if not exist logs mkdir logs

echo [1] 关键文件
for %%F in (APS.exe APS.dll appsettings.json Install-ApsDependencies.ps1) do (
  if exist "%~dp0%%F" (echo   OK  %%F) else (echo   缺  %%F)
)
powershell -NoProfile -Command "$p='Install-ApsDependencies.ps1'; if(Test-Path $p){ $t=Get-Content $p -Raw; if($t -match \"ScriptVersion = '([^']+)'\"){ Write-Host ('  脚本版本: '+$matches[1]) } elseif($t -match 'JsonDocument'){ Write-Host '  警告: 依赖脚本过旧(含 JsonDocument)，请重新 publish 或从 Deployment 复制' } else { Write-Host '  脚本版本: 未知(建议重新 publish)' } }"
echo.

echo [2] .NET 运行时
powershell -NoProfile -Command "$d=(Get-Command dotnet -EA SilentlyContinue); if(-not $d){ Write-Host '  未找到 dotnet 命令'; exit 0 }; & $d.Source --list-runtimes | ForEach-Object { Write-Host ('  '+$_) }"
echo.

echo [3] runtime 安装包
if exist runtime (
  dir /b runtime\*.exe 2>nul || echo   无 exe（需 dotnet-runtime + aspnetcore-runtime）
) else (
  echo   无 runtime 目录
)
echo.

echo [4] 9999 端口占用
powershell -NoProfile -Command "$p=9999; try { $j=Get-Content appsettings.json -Raw|ConvertFrom-Json; $u=$j.Kestrel.Endpoints.Http.Url; if($u -match ':(\d+)\s*$'){ $p=[int]$matches[1] } } catch {}; Write-Host ('  配置端口: '+$p); Get-NetTCPConnection -LocalPort $p -EA SilentlyContinue | Select-Object LocalAddress,State,OwningProcess | Format-Table -AutoSize; if(-not (Get-NetTCPConnection -LocalPort $p -EA SilentlyContinue)){ Write-Host '  当前无进程监听该端口' }"
echo.
echo [4b] Windows 保留端口段（若含 9999 需换端口）
netsh interface ipv4 show excludedportrange protocol=tcp 2>nul | findstr /i "9999 start" || netsh interface ipv4 show excludedportrange protocol=tcp 2>nul
echo.
echo [4c] http.sys URL 保留（含 9999 时可能冲突）
netsh http show urlacl 2>nul | findstr /i "9999" || echo   无 9999 相关 urlacl
echo.

echo [5] appsettings.json 语法
powershell -NoProfile -Command "try { $j=[IO.File]::ReadAllText('appsettings.json'); if([string]::IsNullOrWhiteSpace($j)){ throw 'empty' }; $null=$j|ConvertFrom-Json; Write-Host '  OK  JSON 语法正确' } catch { Write-Host '  错误 JSON 无效:' $_.Exception.Message; Write-Host '  提示: 路径用 D:\\\\共享\\\\目录 ，SQL 用 127.0.0.1\\\\实例名' }"
echo.

echo [6] 最近日志
if exist logs\deps-install.log (
  echo --- deps-install.log 末尾 ---
  powershell -NoProfile -Command "Get-Content logs\deps-install.log -Tail 15"
)
if exist logs\aps-crash.log (
  echo --- aps-crash.log ---
  type logs\aps-crash.log
)
if exist logs\aps-console.log (
  echo --- aps-console.log 末尾 ---
  powershell -NoProfile -Command "Get-Content logs\aps-console.log -Tail 20"
)
echo.
echo 诊断完成。请用 APS-启动.bat 启动（不要直接双击 APS.exe）。
pause
