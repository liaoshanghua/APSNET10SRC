# [Legacy] 源码版 → 公版 全量（Api + lib）。当前协作规则下 Api 以公版为准，请改用：
#   Api:  APSNET10/scripts/Sync-ApiToNet10.ps1（公版 → 源码版）
#   lib:  Sync-LibFromNet10.ps1 或 EasyManufacture.Net10/scripts/Sync-LibToApsNet10.ps1

param(
    [string]$TargetRoot = '',
    [string]$Configuration = 'Release',
    [switch]$SkipBuild,
    [string]$ObfuscatedLibDir = ''
)

$ErrorActionPreference = 'Stop'
$net10 = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($TargetRoot)) {
    $TargetRoot = Join-Path $net10 '..\APSNET10'
}
$TargetRoot = [IO.Path]::GetFullPath($TargetRoot)
$apiSrc = Join-Path $net10 'src\EasyManufacture.Api'
$libDir = Join-Path $TargetRoot 'lib'

if (-not $SkipBuild) {
    Write-Host "Building EasyManufacture.Net10 ($Configuration)..." -ForegroundColor Cyan
    dotnet build (Join-Path $net10 'EasyManufacture.Net10.sln') -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Net10 build failed' }
}

Write-Host "Creating APSNET10 at $TargetRoot" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path (Join-Path $TargetRoot 'src') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $TargetRoot 'scripts') | Out-Null
New-Item -ItemType Directory -Force -Path $libDir | Out-Null

$apiDest = Join-Path $TargetRoot 'src\EasyManufacture.Api'
if (Test-Path $apiDest) { Remove-Item -LiteralPath $apiDest -Recurse -Force }
robocopy $apiSrc $apiDest /E /XD bin obj .vs /XF *.user EasyManufacture.Api.csproj EasyManufacture.LibRefs.props EasyManufacture.LibRuntime.props /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $LASTEXITCODE" }

$apiOut = Join-Path $apiSrc "bin\$Configuration\net10.0-windows"
if (-not (Test-Path $apiOut)) {
    $apiOut = Join-Path $apiSrc "bin\$Configuration\net10.0"
}
if (-not (Test-Path $apiOut)) { throw "Api output not found: $apiOut" }

$libNames = @(
    'EasyManufacture.Domain.dll',
    'EasyManufacture.Application.dll',
    'EasyManufacture.Licence.dll',
    'EasyManufacture.Infrastructure.dll',
    'sapnco.dll',
    'sapnco_utils.dll',
    'Kingdee.CDP.WebApi.SDK.dll'
)

$sourceDir = if ($ObfuscatedLibDir -and (Test-Path $ObfuscatedLibDir)) { $ObfuscatedLibDir } else { $apiOut }
Write-Host "Sync lib from: $sourceDir" -ForegroundColor Cyan

$manifest = [ordered]@{
    version       = '1.0.0'
    syncedUtc     = (Get-Date).ToUniversalTime().ToString('o')
    configuration = $Configuration
    obfuscated    = [bool]($ObfuscatedLibDir -and (Test-Path $ObfuscatedLibDir))
    source        = $sourceDir
    files         = @()
}

foreach ($name in $libNames) {
    $src = Join-Path $sourceDir $name
    if (-not (Test-Path $src)) { throw "Missing lib source: $src" }
    Copy-Item -LiteralPath $src -Destination (Join-Path $libDir $name) -Force
    $fi = Get-Item (Join-Path $libDir $name)
    $hash = (Get-FileHash $fi.FullName -Algorithm SHA256).Hash.Substring(0, 16)
    $manifest.files += @{ name = $name; bytes = $fi.Length; sha256Prefix = $hash }
}

$manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $libDir 'manifest.json') -Encoding UTF8

Write-Host "APSNET10 scaffold complete. Run dotnet build on $TargetRoot" -ForegroundColor Green
