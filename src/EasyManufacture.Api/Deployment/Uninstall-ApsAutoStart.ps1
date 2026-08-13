# 禁止 / 卸载 APS 开机自启（删除 APS + APS-Logon，并写入 Disabled 标记）
# 建议管理员运行：powershell -ExecutionPolicy Bypass -File .\Uninstall-ApiAutoStart.ps1
param(
    [string]$PublishPath = '',
    [string]$TaskName = 'APS',
    [string]$LogonTaskName = 'APS-Logon'
)

$ErrorActionPreference = 'Continue'

function Test-IsAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Remove-TaskIfExists {
    param([string]$Name)
    & schtasks.exe /Query /TN $Name 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        & schtasks.exe /Delete /TN $Name /F 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "已删除计划任务: $Name" -ForegroundColor Green
        }
        else {
            Write-Host "删除失败: $Name（可能需要管理员权限）" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "任务不存在: $Name"
    }
}

if (-not $PublishPath) {
    if ($PSScriptRoot -and (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'APS.exe'))) {
        $PublishPath = $PSScriptRoot
    }
    elseif ($PSScriptRoot -and (Test-Path -LiteralPath (Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\Deployment'))) {
        $PublishPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\bin\Release\net10.0-windows\publish') -ErrorAction SilentlyContinue).Path
    }
    if (-not $PublishPath) { $PublishPath = $PSScriptRoot }
}

Remove-TaskIfExists -Name $TaskName
Remove-TaskIfExists -Name $LogonTaskName

if ($PublishPath -and (Test-Path -LiteralPath $PublishPath)) {
    $marker = @{
        TaskName      = $TaskName
        LogonTaskName = $LogonTaskName
        PublishPath   = (Resolve-Path -LiteralPath $PublishPath).Path
        TriggerMode   = 'Disabled'
        InstalledAt   = (Get-Date).ToString('o')
    } | ConvertTo-Json
    $markerPath = Join-Path $PublishPath '.autostart-installed.json'
    Set-Content -Path $markerPath -Value $marker -Encoding UTF8
    Write-Host "已写入禁止标记: $markerPath (TriggerMode=Disabled)" -ForegroundColor Green
}

$remain = @()
& schtasks.exe /Query /TN $TaskName 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) { $remain += $TaskName }
& schtasks.exe /Query /TN $LogonTaskName 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) { $remain += $LogonTaskName }

if ($remain.Count -gt 0) {
    Write-Host ''
    Write-Host "仍残留任务: $($remain -join ', ')" -ForegroundColor Yellow
    if (-not (Test-IsAdmin)) {
        Write-Host '请右键以管理员运行本脚本或 APS-禁止开机自启.bat' -ForegroundColor Yellow
        exit 1
    }
    exit 1
}

Write-Host ''
Write-Host '已禁止开机启动。若要恢复，请管理员运行 APS-安装开机自启.bat' -ForegroundColor Cyan
exit 0
