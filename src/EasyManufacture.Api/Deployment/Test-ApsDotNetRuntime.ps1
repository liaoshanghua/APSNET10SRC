# 检测 APS 所需的 .NET 10 运行时（ASP.NET Core + Windows Desktop）
# 退出码: 0=就绪 1=未找到 dotnet 10=未找到 dotnet 命令 2=缺 AspNetCore 3=缺 Desktop 4=两者都缺
param(
    [string]$PublishPath = $PSScriptRoot,
    [string]$MinimumVersion = '10.0.0'
)

function Resolve-PublishRoot {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $PSScriptRoot }
    $clean = $Path.Trim().Trim('"').Trim("'")
    while ($clean.Length -gt 0 -and ($clean[-1] -eq '\' -or $clean[-1] -eq '/')) {
        $clean = $clean.Substring(0, $clean.Length - 1)
    }
    if ([string]::IsNullOrWhiteSpace($clean)) { return $PSScriptRoot }
    if (Test-Path -LiteralPath $clean) {
        return (Resolve-Path -LiteralPath $clean).Path
    }
    return $clean
}

$PublishPath = Resolve-PublishRoot $PublishPath

function Get-DotNetExePath {
    param([string]$Root)
    $pathFile = Join-Path $Root '.dotnet-local-path'
    if (Test-Path $pathFile) {
        $dir = (Get-Content $pathFile -Raw).Trim()
        $exe = Join-Path $dir 'dotnet.exe'
        if (Test-Path $exe) { return $exe }
    }
    foreach ($candidate in @(
            (Join-Path $Root 'runtime\dotnet\dotnet.exe'),
            (Join-Path $env:LOCALAPPDATA 'dotnet\dotnet.exe'),
            (Join-Path ${env:ProgramFiles} 'dotnet\dotnet.exe')
        )) {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

function Test-Framework {
    param([string[]]$Lines, [string]$Framework, [string]$MinVersion)
    $min = [Version]$MinVersion
    foreach ($line in $Lines) {
        if ($line -match "^$([regex]::Escape($Framework))\s+(\S+)") {
            $ver = [Version]$matches[1]
            if ($ver.Major -eq $min.Major -and $ver -ge $min) { return $true }
        }
    }
    return $false
}

$dotnet = Get-DotNetExePath $PublishPath
if (-not $dotnet) { exit 10 }

$lines = @(& $dotnet --list-runtimes 2>$null)
if (-not $lines) { exit 10 }

$hasAspNet = Test-Framework $lines 'Microsoft.AspNetCore.App' $MinimumVersion
$hasDesktop = Test-Framework $lines 'Microsoft.WindowsDesktop.App' $MinimumVersion

if ($hasAspNet -and $hasDesktop) { exit 0 }
if (-not $hasAspNet -and -not $hasDesktop) { exit 4 }
if (-not $hasAspNet) { exit 2 }
exit 3
