# 注册 APS 为 Windows 计划任务：系统启动时运行（无需用户登录，SYSTEM 账户）
# 用法（管理员 PowerShell）：
#   cd D:\publish
#   powershell -ExecutionPolicy Bypass -File .\Install-ApsAutoStart.ps1
param(
    [string]$PublishPath = $PSScriptRoot,
    [string]$TaskName = 'APS',
    [int]$Port = 0,
    [string]$ExeName = 'APS'
)

$ErrorActionPreference = 'Stop'

function Resolve-PublishPath {
    param([string]$Candidate)
    $pathsToTry = @()
    if (-not [string]::IsNullOrWhiteSpace($Candidate)) {
        $clean = $Candidate.Trim().Trim('"').Trim("'").TrimEnd('\')
        if ($clean -match '["<>|?*]') { $clean = $clean -replace '["<>|?*]', '' }
        if ($clean) { $pathsToTry += $clean }
    }
    $pathsToTry += $PSScriptRoot
    foreach ($p in $pathsToTry) {
        if ($p -and (Test-Path -LiteralPath $p)) {
            return (Resolve-Path -LiteralPath $p).Path
        }
    }
    throw "Cannot resolve publish path. ScriptRoot=$PSScriptRoot Candidate=$Candidate"
}

function Test-IsAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-SchTasks {
    param(
        [Parameter(Mandatory)]
        [string[]]$ArgumentList,
        [switch]$AllowFailure
    )
    # schtasks 在任务不存在时也会写 stderr；Stop 模式下会被当成致命错误
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & schtasks.exe @ArgumentList 2>&1 | Out-Null
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
    if (-not $AllowFailure -and $code -ne 0) {
        throw "schtasks exit $code : schtasks.exe $($ArgumentList -join ' ')"
    }
    return $code
}

if (-not (Test-IsAdmin)) {
    Write-Host '[错误] 请以管理员身份运行（右键 -> 以管理员身份运行）' -ForegroundColor Red
    exit 1
}

try {
    $PublishPath = Resolve-PublishPath $PublishPath
}
catch {
    Write-Host "[错误] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
$appSettingsPath = Join-Path $PublishPath 'appsettings.json'

if ($Port -le 0 -and (Test-Path $appSettingsPath)) {
    try {
        $json = Get-Content $appSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($json.AutoStart.Port -and [int]$json.AutoStart.Port -gt 0) {
            $Port = [int]$json.AutoStart.Port
        }
        elseif ($json.Kestrel.Endpoints.Http.Url -match ':(\d+)\s*$') {
            $Port = [int]$Matches[1]
        }
        if ($json.AutoStart.TaskName) { $TaskName = [string]$json.AutoStart.TaskName }
        if ($json.AutoStart.ExeName) { $ExeName = [string]$json.AutoStart.ExeName }
    }
    catch { }
}

if ($Port -le 0) { $Port = 9999 }

$entryExe = "$ExeName.exe"
$entryDll = "$ExeName.dll"
if (-not (Test-Path (Join-Path $PublishPath $entryExe)) -and -not (Test-Path (Join-Path $PublishPath $entryDll))) {
    Write-Host "[错误] 未找到 $entryExe / $entryDll，PublishPath=$PublishPath" -ForegroundColor Red
    exit 1
}

# 生成/刷新 start-api.bat / start-api-min.vbs（与程序内逻辑一致）
$batPath = Join-Path $PublishPath 'start-api.bat'
$vbsPath = Join-Path $PublishPath 'start-api-min.vbs'
$bat = @"
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
set ASPNETCORE_URLS=http://0.0.0.0:$Port
echo [%date% %time%] start-api.bat >> logs\startup.log
if exist "%~dp0$entryExe" (
  "%~dp0$entryExe" >> "%~dp0logs\aps-console.log" 2>&1
) else (
  dotnet "%~dp0$entryDll" >> "%~dp0logs\aps-console.log" 2>&1
)
echo [%date% %time%] exited !ERRORLEVEL! >> "%~dp0logs\startup.log"
"@
$vbs = @'
' APS 计划任务用：最小化启动 start-api.bat
Option Explicit
Dim sh, fso, base
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
base = fso.GetParentFolderName(WScript.ScriptFullName)
If Right(base, 1) <> "\" Then base = base & "\"
sh.CurrentDirectory = base
sh.Run "cmd /c """ & base & "start-api.bat""", 7, False
'@
[System.IO.File]::WriteAllText($batPath, $bat, (New-Object System.Text.UTF8Encoding $true))
[System.IO.File]::WriteAllText($vbsPath, $vbs, (New-Object System.Text.UTF8Encoding $false))

# 删除旧任务（不存在时 Query 会报错，属正常情况）
if ((Invoke-SchTasks -ArgumentList @('/Query', '/TN', $TaskName) -AllowFailure) -eq 0) {
    Invoke-SchTasks -ArgumentList @('/Delete', '/TN', $TaskName, '/F') -AllowFailure | Out-Null
}

# 系统启动时 / SYSTEM；用 VBS 最小化启动，避免前台 cmd 被误关
$tr = "wscript.exe //B `"$vbsPath`""
try {
    Invoke-SchTasks -ArgumentList @('/Create', '/F', '/TN', $TaskName, '/TR', $tr, '/SC', 'ONSTART', '/RU', 'SYSTEM', '/RL', 'HIGHEST')
}
catch {
    Write-Host "[错误] schtasks 注册失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$marker = @{
    TaskName    = $TaskName
    PublishPath = $PublishPath
    TriggerMode = 'AtStartup'
    InstalledAt = (Get-Date).ToString('o')
} | ConvertTo-Json
Set-Content -Path (Join-Path $PublishPath '.autostart-installed.json') -Value $marker -Encoding UTF8

Write-Host ''
Write-Host "已注册计划任务: $TaskName" -ForegroundColor Green
Write-Host "  触发: 系统启动时（无需登录）"
Write-Host "  账户: SYSTEM"
Write-Host "  启动: $vbsPath （最小化窗口）"
Write-Host "  脚本: $batPath"
Write-Host "  端口: $Port"
Write-Host ''
Write-Host '验证: 任务计划程序 -> 任务计划程序库 -> APS' -ForegroundColor Cyan
Write-Host '日志: publish\logs\startup.log 与 aps-console.log' -ForegroundColor Cyan
Write-Host ''
Write-Host '提示: 共享目录请用 UNC 路径；runtime 需已安装 aspnetcore-runtime。' -ForegroundColor Yellow
