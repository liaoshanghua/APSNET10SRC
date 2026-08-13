# 检查并自动安装 APS 运行依赖（.NET ASP.NET Core 运行时等）
# 源码位置：src/EasyManufacture.Api/Deployment/（publish 时复制到发布目录）
param(
    [string]$PublishPath = $PSScriptRoot,
    [string]$Channel = '10.0',
    [string]$MinimumVersion = '10.0.0',
    [string]$LocalRuntimeDir = '',
    [switch]$OfflineFirst = $true,
    [switch]$AllowOnlineInstall = $true,
    [switch]$Force
)

$ScriptVersion = '2026-06-04'

$ErrorActionPreference = 'Continue'

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
    throw "Cannot resolve publish path. ScriptRoot=$PSScriptRoot"
}

try {
    $PublishPath = Resolve-PublishPath $PublishPath
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    exit 1
}

$appSettingsPath = Join-Path $PublishPath 'appsettings.json'
$dependenciesEnabled = $true

if (Test-Path $appSettingsPath) {
    try {
        $rawJson = Get-Content $appSettingsPath -Raw -Encoding UTF8
        # appsettings 常含 // 注释，Windows PowerShell ConvertFrom-Json 不支持
        $rawJson = [regex]::Replace($rawJson, '(?m)^\s*//.*?$', '')
        $rawJson = [regex]::Replace($rawJson, ',(\s*\})', '$1')
        $rawJson = [regex]::Replace($rawJson, ',(\s*\])', '$1')
        $appJson = $rawJson | ConvertFrom-Json
        $dep = $appJson.Dependencies
        if ($dep) {
            if ($null -ne $dep.Enabled) { $dependenciesEnabled = [bool]$dep.Enabled }
            if (-not $PSBoundParameters.ContainsKey('Channel') -and $dep.DotNet -and $dep.DotNet.Channel) {
                $Channel = [string]$dep.DotNet.Channel
            }
            if (-not $PSBoundParameters.ContainsKey('MinimumVersion') -and $dep.DotNet -and $dep.DotNet.MinimumVersion) {
                $MinimumVersion = [string]$dep.DotNet.MinimumVersion
            }
            if (-not $PSBoundParameters.ContainsKey('LocalRuntimeDir') -and $dep.LocalRuntimePath) {
                $LocalRuntimeDir = [string]$dep.LocalRuntimePath
            }
            if (-not $PSBoundParameters.ContainsKey('OfflineFirst') -and $null -ne $dep.OfflineFirst) {
                $OfflineFirst = [bool]$dep.OfflineFirst
            }
            if (-not $PSBoundParameters.ContainsKey('AllowOnlineInstall') -and $null -ne $dep.AllowOnlineInstall) {
                $AllowOnlineInstall = [bool]$dep.AllowOnlineInstall
            }
        }
    }
    catch {
        Write-Warning "Failed to read appsettings.json: $($_.Exception.Message)"
    }
}

$logDir = Join-Path $PublishPath 'logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir 'deps-install.log'
$dotnetPathFile = Join-Path $PublishPath '.dotnet-local-path'

function Write-Log {
    param([string]$Message)
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    try { Add-Content -Path $logFile -Value $line -Encoding UTF8 } catch { }
    Write-Host $line
}

function Get-ConfiguredPort {
    $port = 9999
    if (-not (Test-Path $appSettingsPath)) { return $port }
    try {
        $appJson = Get-Content $appSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($appJson.AutoStart.Port -and [int]$appJson.AutoStart.Port -gt 0) { return [int]$appJson.AutoStart.Port }
        $kestrelUrl = $appJson.Kestrel.Endpoints.Http.Url
        if ($kestrelUrl -match ':(\d+)\s*$') { return [int]$matches[1] }
    }
    catch { }
    return $port
}

function Ensure-StartApiBat {
    $batPath = Join-Path $PublishPath 'start-api.bat'
    if (Test-Path $batPath) { return }
    $port = Get-ConfiguredPort
    $batContent = @"
@echo off
cd /d "%~dp0"
if not exist logs mkdir logs
rem Do NOT redirect to deps-install.log - deadlocks with Add-Content inside the ps1.
if exist "%~dp0Install-ApsDependencies.ps1" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" >> logs\deps-console.log 2>&1
)
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:$port
if exist "%~dp0APS.exe" (start "APS" /MIN "%~dp0APS.exe") else (start "APS" /MIN dotnet "%~dp0APS.dll")
"@
    [System.IO.File]::WriteAllText($batPath, $batContent, (New-Object System.Text.UTF8Encoding $false))
    Write-Log "Created missing start-api.bat (port $port)"
}

function Get-ResolvedRuntimeDir {
    if (-not [string]::IsNullOrWhiteSpace($LocalRuntimeDir)) {
        $custom = Join-Path $PublishPath $LocalRuntimeDir
        if (Test-Path $custom) { return (Resolve-Path $custom).Path }
    }
    foreach ($name in @('runtime', 'deps\runtime', 'deps')) {
        $candidate = Join-Path $PublishPath $name
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }
    return $null
}

function Test-RuntimeFolderHasPackages {
    param([string]$RuntimeDir)
    foreach ($p in @('dotnet-runtime-*.exe', 'aspnetcore-runtime-*.exe', 'windowsdesktop-runtime-*.exe', 'dotnet-install.ps1')) {
        if (Get-ChildItem -Path $RuntimeDir -Filter $p -File -ErrorAction SilentlyContinue | Select-Object -First 1) {
            return $true
        }
    }
    return (Test-Path (Join-Path $RuntimeDir 'dotnet\dotnet.exe'))
}

function Get-DotNetExe {
    $candidates = @()
    if (Test-Path $dotnetPathFile) {
        $candidates += (Join-Path ((Get-Content $dotnetPathFile -Raw).Trim()) 'dotnet.exe')
    }
    $candidates += @(
        (Join-Path $PublishPath 'runtime\dotnet\dotnet.exe'),
        (Join-Path $env:LOCALAPPDATA 'dotnet\dotnet.exe'),
        (Join-Path ${env:ProgramFiles} 'dotnet\dotnet.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'dotnet\dotnet.exe')
    )
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { $candidates += $cmd.Source }
    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) { return $path }
    }
    return $null
}

function Test-DotNetRuntime {
    param([string]$Framework, [string]$MinVersion)
    $dotnet = Get-DotNetExe
    if (-not $dotnet) { return $false }
    try {
        $min = [Version]$MinVersion
        $lines = & $dotnet --list-runtimes 2>$null
        if (-not $lines) { return $false }
        foreach ($line in $lines) {
            if ($line -match "^$([regex]::Escape($Framework))\s+(\S+)") {
                $ver = [Version]$matches[1]
                if ($ver.Major -eq $min.Major -and $ver -ge $min) { return $true }
            }
        }
    }
    catch {
        Write-Log "WARN: runtime check failed: $($_.Exception.Message)"
    }
    return $false
}

function Test-ApsRuntimesReady {
    param([string]$MinVersion)
    return (Test-DotNetRuntime 'Microsoft.NETCore.App' $MinVersion) `
        -and (Test-DotNetRuntime 'Microsoft.AspNetCore.App' $MinVersion) `
        -and (Test-DotNetRuntime 'Microsoft.WindowsDesktop.App' $MinVersion)
}

function Test-ApsPublishFiles {
    $missing = @()
    foreach ($name in @('APS.dll', 'appsettings.json')) {
        if (-not (Test-Path (Join-Path $PublishPath $name))) { $missing += $name }
    }
    if ($missing.Count -gt 0) {
        Write-Log ("ERROR: missing required files: {0}" -f ($missing -join ', '))
        return $false
    }
    return $true
}

function Find-LocalInstaller {
    param([string]$RuntimeDir, [string[]]$Patterns)
    foreach ($pattern in $Patterns) {
        $files = @(Get-ChildItem -Path $RuntimeDir -Filter $pattern -File -ErrorAction SilentlyContinue)
        if ($files.Count -gt 0) {
            return ($files | Sort-Object Name -Descending | Select-Object -First 1).FullName
        }
    }
    return $null
}

function Install-ViaLocalExe {
    param([string]$RuntimeDir)
    $dotnetExe = Find-LocalInstaller $RuntimeDir @('dotnet-runtime-*-win-x64.exe', 'dotnet-runtime-*-win-x86.exe')
    $aspnetExe = Find-LocalInstaller $RuntimeDir @('aspnetcore-runtime-*-win-x64.exe', 'aspnetcore-runtime-*-win-x86.exe')
    $desktopExe = Find-LocalInstaller $RuntimeDir @('windowsdesktop-runtime-*-win-x64.exe', 'windowsdesktop-runtime-*-win-x86.exe')
    if (-not $dotnetExe -and -not $aspnetExe -and -not $desktopExe) { return $false }
    foreach ($exe in @($dotnetExe, $aspnetExe, $desktopExe)) {
        if (-not $exe) { continue }
        Write-Log "Installing local package: $(Split-Path $exe -Leaf)"
        try {
            $proc = Start-Process -FilePath $exe -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru -ErrorAction Stop
            Write-Log "Installer exit code $($proc.ExitCode): $(Split-Path $exe -Leaf)"
        }
        catch {
            Write-Log "ERROR: local installer failed: $($_.Exception.Message)"
            return $false
        }
    }
    return $true
}

function Use-LocalExtractedRuntime {
    param([string]$RuntimeDir)
    $dotnetExe = Join-Path $RuntimeDir 'dotnet\dotnet.exe'
    if (-not (Test-Path $dotnetExe)) { return $false }
    $dotnetRoot = Split-Path $dotnetExe -Parent
    Set-Content -Path $dotnetPathFile -Value $dotnetRoot -Encoding ASCII -NoNewline
    Write-Log "Using bundled runtime: $dotnetRoot"
    return $true
}

function Install-ViaDotNetInstallScript {
    param([string]$ScriptPath, [string]$RuntimeChannel, [string]$InstallDir)
    if (-not (Test-Path $ScriptPath)) { return $false }
    Write-Log "Running dotnet-install.ps1 -> $InstallDir"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $ScriptPath -Runtime dotnet -Channel $RuntimeChannel -InstallDir $InstallDir -NoPath
    & powershell -NoProfile -ExecutionPolicy Bypass -File $ScriptPath -Runtime aspnetcore -Channel $RuntimeChannel -InstallDir $InstallDir -NoPath
    & powershell -NoProfile -ExecutionPolicy Bypass -File $ScriptPath -Runtime windowsdesktop -Channel $RuntimeChannel -InstallDir $InstallDir -NoPath
    Set-Content -Path $dotnetPathFile -Value $InstallDir -Encoding ASCII -NoNewline
    return $true
}

function Install-ViaWinget {
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) { return $false }
    foreach ($id in @('Microsoft.DotNet.AspNetCore.10', 'Microsoft.AspNetCore.App.10')) {
        Write-Log "Trying winget: $id"
        & winget install --id $id -e --accept-source-agreements --accept-package-agreements --silent 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) { return $true }
    }
    return $false
}

try {
    Write-Log "Install-ApsDependencies.ps1 v$ScriptVersion"
    Write-Log "Checking APS dependencies in $PublishPath"
    Ensure-StartApiBat

    if (-not $dependenciesEnabled) {
        Write-Log 'Dependencies check disabled in appsettings.'
        exit 0
    }

    if (-not (Test-ApsPublishFiles)) { exit 1 }

    $needAspNet = -not (Test-DotNetRuntime -Framework 'Microsoft.AspNetCore.App' -MinVersion $MinimumVersion)
    $needCore = -not (Test-DotNetRuntime -Framework 'Microsoft.NETCore.App' -MinVersion $MinimumVersion)
    $needDesktop = -not (Test-DotNetRuntime -Framework 'Microsoft.WindowsDesktop.App' -MinVersion $MinimumVersion)

    if (-not $Force -and (Test-ApsRuntimesReady $MinimumVersion)) {
        Write-Log ".NET $MinimumVersion+ (Core + ASP.NET Core + Windows Desktop) already installed."
        exit 0
    }

    Write-Log "Missing .NET $MinimumVersion+ runtimes (Core=$needCore, AspNet=$needAspNet, Desktop=$needDesktop). Installing ..."
    $runtimeDir = Get-ResolvedRuntimeDir
    if ($runtimeDir) { Write-Log "Runtime folder: $runtimeDir" }

    if ($runtimeDir -and -not (Test-RuntimeFolderHasPackages $runtimeDir)) {
        Write-Log 'WARN: runtime/ exists but required installers missing.'
        Write-Log '      Need THREE: dotnet-runtime, aspnetcore-runtime, windowsdesktop-runtime (*-win-x64.exe)'
        $sdkOnly = @(Get-ChildItem -Path $runtimeDir -Filter 'dotnet-sdk-*-win-*.exe' -File -ErrorAction SilentlyContinue)
        if ($sdkOnly.Count -gt 0) {
            Write-Log "HINT: dotnet-sdk is SDK only; need aspnetcore-runtime + windowsdesktop-runtime"
        }
    }

    $installed = $false

    if ($OfflineFirst -and $runtimeDir) {
        if (Use-LocalExtractedRuntime $runtimeDir) { Start-Sleep 2; $installed = (Test-ApsRuntimesReady $MinimumVersion) }
        if (-not $installed -and (Install-ViaLocalExe $runtimeDir)) { Start-Sleep 5; $installed = (Test-ApsRuntimesReady $MinimumVersion) }
        $localScript = Join-Path $runtimeDir 'dotnet-install.ps1'
        if (-not $installed -and (Test-Path $localScript)) {
            Install-ViaDotNetInstallScript $localScript $Channel (Join-Path $env:LOCALAPPDATA 'dotnet') | Out-Null
            Start-Sleep 2
            $installed = (Test-ApsRuntimesReady $MinimumVersion)
        }
    }

    if (-not $installed -and $AllowOnlineInstall) {
        if (Install-ViaWinget) { Start-Sleep 5; $installed = (Test-ApsRuntimesReady $MinimumVersion) }
        if (-not $installed) {
            $toolsDir = Join-Path $PublishPath '.tools'
            if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null }
            $onlineScript = Join-Path $toolsDir 'dotnet-install.ps1'
            if (-not (Test-Path $onlineScript)) {
                Write-Log 'Downloading dotnet-install.ps1 ...'
                Invoke-WebRequest -Uri 'https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1' -OutFile $onlineScript -UseBasicParsing
            }
            Install-ViaDotNetInstallScript $onlineScript $Channel (Join-Path $env:LOCALAPPDATA 'dotnet') | Out-Null
            Start-Sleep 2
            $installed = (Test-ApsRuntimesReady $MinimumVersion)
        }
    }

    if (-not $installed) {
        Write-Log 'ERROR: .NET 10 install failed.'
        if ($runtimeDir) {
            $hasDotnet = [bool](Find-LocalInstaller $runtimeDir @('dotnet-runtime-*-win-x64.exe'))
            $hasAspnet = [bool](Find-LocalInstaller $runtimeDir @('aspnetcore-runtime-*-win-x64.exe'))
            $hasDesktop = [bool](Find-LocalInstaller $runtimeDir @('windowsdesktop-runtime-*-win-x64.exe'))
            if ($hasDotnet -and -not $hasAspnet) {
                Write-Log "HINT: have dotnet-runtime but missing aspnetcore-runtime-*-win-x64.exe"
            }
            if (-not $hasDesktop) {
                Write-Log "HINT: missing windowsdesktop-runtime-*-win-x64.exe (APS tray needs WinForms)"
            }
        }
        Write-Log "  1) Put THREE installers in runtime/ [offline]"
        Write-Log "  2) Publish with: .\scripts\Publish-Aps.ps1 -WithRuntime"
        Write-Log "  3) Manual: https://dotnet.microsoft.com/download/dotnet/10.0"
        exit 1
    }

    Write-Log 'Dependencies OK.'
    exit 0
}
catch {
    Write-Log "FATAL: $($_.Exception.Message)"
    Write-Log $_.ScriptStackTrace
    exit 1
}
