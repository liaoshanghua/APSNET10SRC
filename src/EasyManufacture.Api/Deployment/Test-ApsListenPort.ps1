# 检查 appsettings 中配置的 Kestrel 端口是否已被占用
# 退出码: 0=可用 1=被占用
param(
    [string]$PublishPath = $PSScriptRoot,
    [switch]$KillApsOnly
)

function Resolve-PublishRoot {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $PSScriptRoot }
    $clean = $Path.Trim().Trim('"').Trim("'")
    while ($clean.Length -gt 0 -and ($clean[-1] -eq '\' -or $clean[-1] -eq '/')) {
        $clean = $clean.Substring(0, $clean.Length - 1)
    }
    if ([string]::IsNullOrWhiteSpace($clean)) { return $PSScriptRoot }
    if (Test-Path -LiteralPath $clean) { return (Resolve-Path -LiteralPath $clean).Path }
    return $clean
}

function Get-ConfiguredPort {
    param([string]$Root)
    $port = 9999
    $cfg = Join-Path $Root 'appsettings.json'
    if (-not (Test-Path -LiteralPath $cfg)) { return $port }
    try {
        $j = Get-Content -LiteralPath $cfg -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($j.AutoStart.Port -and [int]$j.AutoStart.Port -gt 0) { return [int]$j.AutoStart.Port }
        $url = $j.Kestrel.Endpoints.Http.Url
        if ($url -match ':(\d+)\s*$') { return [int]$matches[1] }
    }
    catch { }
    return $port
}

function Get-ListenerProcessIds {
    param([int]$Port)
    @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique)
}

function Test-ProcessFromInstallRoot {
    param(
        [int]$ProcessId,
        [string]$InstallRoot
    )
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$ProcessId" -ErrorAction SilentlyContinue
    if (-not $proc) { return $false }
    if (-not $proc.ExecutablePath) {
        return ($proc.Name -eq 'APS')
    }
    $exeDir = [IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($proc.ExecutablePath))
    $root = [IO.Path]::GetFullPath($InstallRoot)
    return $exeDir.Equals($root, [StringComparison]::OrdinalIgnoreCase)
}

function Stop-InstallListeners {
    param(
        [string]$InstallRoot,
        [int]$Port
    )
    $pids = Get-ListenerProcessIds -Port $Port
    if ($pids.Count -eq 0) { return $true }

    $stoppedAny = $false
    foreach ($procId in $pids) {
        if (-not (Test-ProcessFromInstallRoot -ProcessId $procId -InstallRoot $InstallRoot)) {
            $other = Get-CimInstance Win32_Process -Filter "ProcessId=$procId" -ErrorAction SilentlyContinue
            $path = if ($other) { $other.ExecutablePath } else { '?' }
            Write-Host "跳过 PID $procId ($path)，不属于本安装目录，端口 $Port 可能被其他程序占用。" -ForegroundColor Yellow
            continue
        }
        Write-Host "正在结束本实例 PID $procId (端口 $Port) ..."
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        $stoppedAny = $true
    }

    if ($stoppedAny) { Start-Sleep -Seconds 2 }
    return (@(Get-ListenerProcessIds -Port $Port).Count -eq 0)
}

$root = Resolve-PublishRoot $PublishPath
$port = Get-ConfiguredPort $root

$listeners = @(Get-ListenerProcessIds -Port $port)
if ($listeners.Count -eq 0) { exit 0 }

Write-Host ""
Write-Host "[错误] 端口 $port 已被占用，APS 无法启动。" -ForegroundColor Red
Write-Host "  安装目录: $root"
Write-Host ""

foreach ($procId in $listeners) {
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$procId" -ErrorAction SilentlyContinue
    $name = if ($proc) { $proc.Name } else { '?' }
    $path = if ($proc) { $proc.ExecutablePath } else { '?' }
    $tag = if (Test-ProcessFromInstallRoot -ProcessId $procId -InstallRoot $root) { '本实例' } else { '其他程序' }
    Write-Host "  PID $procId  ($name) [$tag] $path"
}

Write-Host ""
Write-Host "处理："
Write-Host "  1) 在本目录运行 APS-结束旧进程.bat（仅结束占用端口 $port 的本实例）"
Write-Host "  2) 或修改 appsettings.json 中 Kestrel 端口"
Write-Host "  3) 勿使用 taskkill /IM APS.exe /F（会误杀其他端口的 APS 实例）"
Write-Host ""

if ($KillApsOnly) {
    if (Stop-InstallListeners -InstallRoot $root -Port $port) {
        Write-Host ('端口 {0} 已释放（本安装目录）。' -f $port) -ForegroundColor Green
        exit 0
    }
}

exit 1
