# 一键发布 APS（含部署脚本，发布目录可直接拷到服务器运行）

param(
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\publish\api'),
    [switch]$WithRuntime,
    [switch]$CompressWwwroot,
    [ValidateSet('x64', 'x86', 'arm64')]
    [string]$Arch = 'x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$apiProject = Join-Path $root 'src\EasyManufacture.Api\EasyManufacture.Api.csproj'
$depsRuntime = Join-Path $root 'deps\dotnet'
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$publishRuntime = Join-Path $OutputDir 'runtime'

function Copy-RuntimePack {
    param(
        [string]$SourceDir,
        [string]$DestDir
    )
    if (-not (Test-Path $SourceDir)) { return 0 }
    if (-not (Test-Path $DestDir)) {
        New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
    }
    $count = 0
    foreach ($pattern in @('dotnet-runtime-*-win-*.exe', 'aspnetcore-runtime-*-win-*.exe', 'windowsdesktop-runtime-*-win-*.exe', 'dotnet-install.ps1')) {
        Get-ChildItem -Path $SourceDir -Filter $pattern -File -ErrorAction SilentlyContinue | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $DestDir -Force
            $count++
        }
    }
    $portable = Join-Path $SourceDir 'dotnet\dotnet.exe'
    if (Test-Path $portable) {
        Copy-Item -Path (Join-Path $SourceDir 'dotnet') -Destination (Join-Path $DestDir 'dotnet') -Recurse -Force
        $count++
    }
    return $count
}

function Test-RuntimePackComplete {
    param([string]$Dir)
    if (-not (Test-Path $Dir)) { return $false }
    if (Test-Path (Join-Path $Dir 'dotnet\dotnet.exe')) { return $true }
    $hasDotnet = @(Get-ChildItem -Path $Dir -Filter 'dotnet-runtime-*-win-*.exe' -File -ErrorAction SilentlyContinue).Count -gt 0
    $hasAspnet = @(Get-ChildItem -Path $Dir -Filter 'aspnetcore-runtime-*-win-*.exe' -File -ErrorAction SilentlyContinue).Count -gt 0
    $hasDesktop = @(Get-ChildItem -Path $Dir -Filter 'windowsdesktop-runtime-*-win-*.exe' -File -ErrorAction SilentlyContinue).Count -gt 0
    return ($hasDotnet -and $hasAspnet -and $hasDesktop)
}

if (-not (Test-Path $depsRuntime)) {
    New-Item -ItemType Directory -Path $depsRuntime -Force | Out-Null
}

if ($WithRuntime -and -not (Test-RuntimePackComplete $depsRuntime)) {
    Write-Host "Downloading .NET runtime pack -> $depsRuntime" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'Download-DotNetRuntimePack.ps1') -OutputDir $depsRuntime -Arch $Arch
}

Write-Host "Publishing APS -> $OutputDir" -ForegroundColor Cyan
dotnet publish $apiProject -c Release -o $OutputDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$ensureBom = Join-Path $root 'src\EasyManufacture.Api\Deployment\Ensure-DeploymentPs1Utf8Bom.ps1'
if (Test-Path -LiteralPath $ensureBom) {
    $deploySrc = Join-Path $root 'src\EasyManufacture.Api\Deployment'
    & $ensureBom -TargetDir $deploySrc
    & $ensureBom -TargetDir $OutputDir
}

$deployRuntime = Join-Path $root 'src\EasyManufacture.Api\Deployment\runtime'
Copy-RuntimePack -SourceDir $deployRuntime -DestDir $publishRuntime | Out-Null
Copy-RuntimePack -SourceDir $depsRuntime -DestDir $publishRuntime | Out-Null

$wwwroot = Join-Path $OutputDir 'wwwroot'
if ($CompressWwwroot -and (Test-Path $wwwroot)) {
    Write-Host "Compressing wwwroot static files..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'Compress-Wwwroot.ps1') -WwwRoot $wwwroot
}

Write-Host ''
Write-Host 'Publish OK. Deployment files included:' -ForegroundColor Green
@(
    'APS.exe',
    'APS-启动.bat',
    'start-api.bat',
    'start-api-min.vbs',
    'Install-ApsDependencies.ps1',
    'Install-ApsAutoStart.ps1',
    'APS-安装开机自启.bat',
    'runtime\README.txt'
) | ForEach-Object {
    $p = Join-Path $OutputDir $_
    $ok = Test-Path $p
    Write-Host ("  [{0}] {1}" -f $(if ($ok) { 'OK' } else { '!!' }), $_)
}

$runtimeExes = @(Get-ChildItem -Path $publishRuntime -Filter '*.exe' -File -ErrorAction SilentlyContinue)
if ($runtimeExes.Count -gt 0) {
    Write-Host ''
    Write-Host 'Runtime offline pack:' -ForegroundColor Green
    $runtimeExes | ForEach-Object { Write-Host "  [OK] runtime\$($_.Name)" }
}
elseif (-not $WithRuntime) {
    Write-Host ''
    Write-Host 'Tip: put installers in deps\dotnet\ or run with -WithRuntime' -ForegroundColor Yellow
    Write-Host '  deps\dotnet\dotnet-runtime-10.x.x-win-x64.exe' -ForegroundColor DarkGray
    Write-Host '  deps\dotnet\aspnetcore-runtime-10.x.x-win-x64.exe' -ForegroundColor DarkGray
}

Write-Host ''
Write-Host "Server: copy folder -> run APS-启动.bat (port 9999)" -ForegroundColor Cyan
