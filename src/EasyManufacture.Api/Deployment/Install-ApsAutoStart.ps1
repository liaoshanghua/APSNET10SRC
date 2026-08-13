# 注册 APS 开机自启：
#   1) APS        — 系统启动 / SYSTEM（先起服务，无托盘）
#   2) APS-Logon  — 用户登录后（结束旧进程，在当前桌面重启，显示托盘）
# 用法（管理员）：powershell -ExecutionPolicy Bypass -File .\Install-ApsAutoStart.ps1
param(
    [string]$PublishPath = $PSScriptRoot,
    [string]$TaskName = 'APS',
    [string]$LogonTaskName = 'APS-Logon',
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

function Remove-TaskIfExists {
    param([string]$Name)
    if ((Invoke-SchTasks -ArgumentList @('/Query', '/TN', $Name) -AllowFailure) -eq 0) {
        Invoke-SchTasks -ArgumentList @('/Delete', '/TN', $Name, '/F') -AllowFailure | Out-Null
    }
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
        $raw = Get-Content $appSettingsPath -Raw -Encoding UTF8
        # Strip // comments and trailing commas (avoid [}\]] which breaks some PS parsers / corrupted copies)
        $raw = [regex]::Replace($raw, '(?m)^\s*//.*?$', '')
        $raw = [regex]::Replace($raw, ',(\s*\})', '$1')
        $raw = [regex]::Replace($raw, ',(\s*\])', '$1')
        $json = $raw | ConvertFrom-Json
        if ($json.AutoStart.Port -and [int]$json.AutoStart.Port -gt 0) {
            $Port = [int]$json.AutoStart.Port
        }
        elseif ($json.Kestrel.Endpoints.Http.Url -match ':(\d+)\s*$') {
            $Port = [int]$Matches[1]
        }
        if ($json.AutoStart.TaskName) { $TaskName = [string]$json.AutoStart.TaskName }
        if ($json.AutoStart.LogonTaskName) { $LogonTaskName = [string]$json.AutoStart.LogonTaskName }
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

$batPath = Join-Path $PublishPath 'start-api.bat'
$vbsPath = Join-Path $PublishPath 'start-api-min.vbs'
$logonBatPath = Join-Path $PublishPath 'start-api-logon.bat'
$userId = "$env:USERDOMAIN\$env:USERNAME"

$bat = @"
@echo off
cd /d "%~dp0"
if not exist logs mkdir logs
if exist "%~dp0Install-ApsDependencies.ps1" (
  powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" >> logs\deps-console.log 2>&1
)
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:$Port
echo [%date% %time%] start-api.bat >> logs\startup.log
if exist "%~dp0$entryExe" (
  start "APS" /MIN "%~dp0$entryExe"
) else (
  start "APS" /MIN dotnet "%~dp0$entryDll"
)
echo [%date% %time%] launched >> logs\startup.log
"@

$logonBat = @"
@echo off
cd /d "%~dp0"
if not exist logs mkdir logs
echo [%date% %time%] logon: restart for tray >> logs\startup.log
taskkill /F /IM $entryExe >nul 2>&1
ping 127.0.0.1 -n 3 >nul
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:$Port
if exist "%~dp0$entryExe" (
  start "APS" "%~dp0$entryExe"
) else (
  start "APS" dotnet "%~dp0$entryDll"
)
echo [%date% %time%] logon: launched >> logs\startup.log
"@

$vbs = @'
' APS boot: minimize start-api.bat
Option Explicit
Dim sh, fso, base
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
base = fso.GetParentFolderName(WScript.ScriptFullName)
If Right(base, 1) <> "\" Then base = base & "\"
sh.CurrentDirectory = base
sh.Run "cmd /c """ & base & "start-api.bat""", 7, True
'@

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$batText = ($bat -replace "(?<!\r)\n", "`r`n")
$logonText = ($logonBat -replace "(?<!\r)\n", "`r`n")
[System.IO.File]::WriteAllText($batPath, $batText, $utf8NoBom)
[System.IO.File]::WriteAllText($logonBatPath, $logonText, $utf8NoBom)
[System.IO.File]::WriteAllText($vbsPath, $vbs, $utf8NoBom)

Remove-TaskIfExists -Name $TaskName
Remove-TaskIfExists -Name $LogonTaskName

$trBoot = "wscript.exe //B `"$vbsPath`""
$trLogon = "cmd.exe /c `"$logonBatPath`""

try {
    Invoke-SchTasks -ArgumentList @('/Create', '/F', '/TN', $TaskName, '/TR', $trBoot, '/SC', 'ONSTART', '/RU', 'SYSTEM', '/RL', 'HIGHEST')
    Invoke-SchTasks -ArgumentList @('/Create', '/F', '/TN', $LogonTaskName, '/TR', $trLogon, '/SC', 'ONLOGON', '/RU', $userId, '/RL', 'HIGHEST')
}
catch {
    Write-Host "[错误] schtasks 注册失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$marker = @{
    TaskName      = $TaskName
    LogonTaskName = $LogonTaskName
    PublishPath   = $PublishPath
    TriggerMode   = 'AtStartup+AtLogOn'
    InstalledAt   = (Get-Date).ToString('o')
} | ConvertTo-Json
Set-Content -Path (Join-Path $PublishPath '.autostart-installed.json') -Value $marker -Encoding UTF8

Write-Host ''
Write-Host "已注册计划任务:" -ForegroundColor Green
Write-Host "  1) $TaskName      — 系统启动 / SYSTEM（先起服务）"
Write-Host "  2) $LogonTaskName — 用户登录 / $userId（重启并显示托盘）"
Write-Host "  启动脚本: $batPath"
Write-Host "  登录脚本: $logonBatPath"
Write-Host "  端口: $Port"
Write-Host ''
Write-Host '验证: 任务计划程序 -> APS / APS-Logon' -ForegroundColor Cyan
Write-Host '日志: publish\logs\startup.log' -ForegroundColor Cyan
Write-Host ''
Write-Host '说明: 开机后接口即可用；登录桌面后会短暂重启一次以出现托盘。' -ForegroundColor Yellow
