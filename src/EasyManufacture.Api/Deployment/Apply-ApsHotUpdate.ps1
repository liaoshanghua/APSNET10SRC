#Requires -Version 5.1
<#
.SYNOPSIS
  将 update\ 目录中的发布包应用到当前 APS 安装目录（停服 → 覆盖 → 启动）。

.DESCRIPTION
  默认从「安装目录\update」读取新版本，覆盖到安装目录。
  不覆盖：appsettings.json、register.ini、wwwroot（前端，除非 -UpdateWwwroot）、logs\、update\、backup\

.EXAMPLE
  # 在服务器 APS 安装目录执行（已先把新包放进 update\）
  .\Apply-ApsHotUpdate.ps1 -Confirm

.EXAMPLE
  .\Apply-ApsHotUpdate.ps1 -UpdateSource "D:\Staging\api" -Backup -Confirm
#>
param(
    [string] $InstallDir = $PSScriptRoot,

    [string] $UpdateSource = '',

    [switch] $NoStart,

    [switch] $Backup,

    [switch] $Confirm,

    [switch] $UpdateWwwroot
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
    Write-Host $line
    if ($script:LogFile) {
        Add-Content -LiteralPath $script:LogFile -Value $line -Encoding UTF8
    }
}

function Stop-ApsProcess {
    param([string]$Root)

    $port = 9999
    $cfg = Join-Path $Root 'appsettings.json'
    if (Test-Path -LiteralPath $cfg) {
        try {
            $j = Get-Content -LiteralPath $cfg -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($j.AutoStart.Port -and [int]$j.AutoStart.Port -gt 0) { $port = [int]$j.AutoStart.Port }
            elseif ($j.Kestrel.Endpoints.Http.Url -match ':(\d+)\s*$') { $port = [int]$matches[1] }
        }
        catch { }
    }

    $killScript = Join-Path $Root 'Test-ApsListenPort.ps1'
    if (Test-Path -LiteralPath $killScript) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $killScript -PublishPath $Root -KillApsOnly | Out-Host
        $stillListen = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Where-Object { Test-ProcessFromInstallRoot -ProcessId $_.OwningProcess -InstallRoot $Root })
        if ($stillListen.Count -gt 0) {
            throw "端口 $port 仍被本实例占用，请先手动结束或检查 appsettings.json"
        }
        return
    }

    # 无脚本时：仅结束占用本端口且 exe 在本目录的进程
    $listeners = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
    foreach ($conn in $listeners) {
        $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$($conn.OwningProcess)" -ErrorAction SilentlyContinue
        if (-not $proc -or -not $proc.ExecutablePath) { continue }
        $exeDir = [IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($proc.ExecutablePath))
        $rootFull = [IO.Path]::GetFullPath($Root)
        if ($exeDir.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
            Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 2
}

function Test-ProcessFromInstallRoot {
    param([int]$ProcessId, [string]$InstallRoot)
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$ProcessId" -ErrorAction SilentlyContinue
    if (-not $proc -or -not $proc.ExecutablePath) { return $false }
    $exeDir = [IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($proc.ExecutablePath))
    $root = [IO.Path]::GetFullPath($InstallRoot)
    return $exeDir.Equals($root, [StringComparison]::OrdinalIgnoreCase)
}

function Test-ApsPing {
    param([string]$Root, [int]$Retries = 12)

    $port = 9999
    $cfg = Join-Path $Root 'appsettings.json'
    if (Test-Path -LiteralPath $cfg) {
        try {
            $j = Get-Content -LiteralPath $cfg -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($j.AutoStart.Port -and [int]$j.AutoStart.Port -gt 0) { $port = [int]$j.AutoStart.Port }
            elseif ($j.Kestrel.Endpoints.Http.Url -match ':(\d+)\s*$') { $port = [int]$matches[1] }
        }
        catch { }
    }

    $url = "http://127.0.0.1:$port/APSAPI/Ping"
    for ($i = 1; $i -le $Retries; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
            if ($resp.StatusCode -eq 200) { return $true }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }
    return $false
}

function Resolve-DeployPath {
    param(
        [string]$Path,
        [string]$Fallback = $PSScriptRoot
    )
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return [System.IO.Path]::GetFullPath($Fallback)
    }
    # BAT 传 "%~dp0" 时末尾 \ 会使 \" 转义引号，路径变成 D:\dir" 等非法字符
    $clean = $Path.Trim().Trim('"', ' ', "`t", "`r", "`n").TrimEnd('\')
    if ([string]::IsNullOrWhiteSpace($clean)) {
        return [System.IO.Path]::GetFullPath($Fallback)
    }
    return [System.IO.Path]::GetFullPath($clean)
}

$InstallDir = Resolve-DeployPath -Path $InstallDir
if ([string]::IsNullOrWhiteSpace($UpdateSource)) {
    $UpdateSource = Join-Path $InstallDir 'update'
}
else {
    $UpdateSource = Resolve-DeployPath -Path $UpdateSource
}

$logsDir = Join-Path $InstallDir 'logs'
if (-not (Test-Path -LiteralPath $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
}
$script:LogFile = Join-Path $logsDir 'hot-update.log'

Write-Log '========== APS 热更新开始 =========='
Write-Log "安装目录: $InstallDir"
Write-Log "更新来源: $UpdateSource"

if (-not (Test-Path -LiteralPath $UpdateSource)) {
    throw "更新目录不存在: $UpdateSource`n请先将 publish 结果复制到安装目录下的 update\ 文件夹。"
}

$hasExe = Test-Path -LiteralPath (Join-Path $UpdateSource 'APS.exe')
$hasDll = Test-Path -LiteralPath (Join-Path $UpdateSource 'APS.dll')
if (-not $hasExe -and -not $hasDll) {
    throw "更新目录中未找到 APS.exe 或 APS.dll，请确认 publish 包是否完整。"
}

if (-not $Confirm) {
    Write-Host ''
    Write-Host '将停止 APS，用 update 目录覆盖程序文件（保留 appsettings.json / register.ini）。' -ForegroundColor Yellow
    $answer = Read-Host '请输入 YES 继续'
    if ($answer -ne 'YES') {
        Write-Log '用户取消。'
        exit 0
    }
}

Write-Log '正在停止 APS...'
Stop-ApsProcess -Root $InstallDir
Write-Log 'APS 已停止。'

if ($Backup) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupDir = Join-Path $InstallDir "backup\$stamp"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Write-Log "备份当前程序到 $backupDir ..."
    Get-ChildItem -LiteralPath $InstallDir -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in '.exe', '.dll', '.json', '.config' -and $_.Name -notin 'appsettings.json', 'register.ini' } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $backupDir -Force }
    if (Test-Path -LiteralPath (Join-Path $InstallDir 'wwwroot')) {
        Copy-Item -LiteralPath (Join-Path $InstallDir 'wwwroot') -Destination (Join-Path $backupDir 'wwwroot') -Recurse -Force
    }
}

Write-Log '正在复制文件...'
$robocopyArgs = @(
    $UpdateSource,
    $InstallDir,
    '/E',
    '/IS',
    '/IT',
    '/R:3',
    '/W:5',
    '/NFL',
    '/NDL',
    '/NP',
    '/XF', 'appsettings.json', 'register.ini',
    '/XD', 'logs', 'update', 'backup'
)
if (-not $UpdateWwwroot) {
    $robocopyArgs += '/XD', 'wwwroot'
    Write-Log '保留现有 wwwroot（前端）。若要覆盖请加 -UpdateWwwroot。'
}
& robocopy @robocopyArgs | Out-Host
$rc = $LASTEXITCODE
if ($rc -ge 8) {
    throw "robocopy 失败，退出码 $rc"
}
Write-Log "文件复制完成（robocopy 退出码 $rc）。"

if ($NoStart) {
    Write-Log '已跳过启动（-NoStart）。请手动运行 APS-启动.bat。'
    exit 0
}

$starter = Join-Path $InstallDir 'start-api-min.vbs'
if (-not (Test-Path -LiteralPath $starter)) {
    $starter = Join-Path $InstallDir 'APS-启动.bat'
}
if (-not (Test-Path -LiteralPath $starter)) {
    throw '找不到 start-api-min.vbs 或 APS-启动.bat，无法自动启动。'
}

Write-Log '正在启动 APS...'
if ($starter.EndsWith('.vbs', [StringComparison]::OrdinalIgnoreCase)) {
    Start-Process -FilePath 'wscript.exe' -ArgumentList "`"$starter`"" -WorkingDirectory $InstallDir
}
else {
    Start-Process -FilePath $starter -WorkingDirectory $InstallDir
}

Start-Sleep -Seconds 4
if (Test-ApsPing -Root $InstallDir) {
    Write-Log '热更新成功，/APSAPI/Ping 已响应。'
}
else {
    Write-Warning 'APS 已启动，但 Ping 暂未响应，请查看 logs\aps-console.log'
}

Write-Log '========== APS 热更新结束 =========='
