# APSNET10 Api -> EasyManufacture.Net10 (Api maintained in public repo)
# Does NOT overwrite EasyManufacture.Api.csproj (public uses lib/, source uses ProjectReference)

param(
    [string]$ApsNet10Root = '',
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$net10 = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($ApsNet10Root)) {
    $ApsNet10Root = Join-Path $net10 '..\APSNET10'
}
$ApsNet10Root = [IO.Path]::GetFullPath($ApsNet10Root)

$apiSrc = Join-Path $ApsNet10Root 'src\EasyManufacture.Api'
$apiDest = Join-Path $net10 'src\EasyManufacture.Api'

if (-not (Test-Path -LiteralPath $apiSrc)) {
    throw "Public Api folder not found: $apiSrc"
}
if (-not (Test-Path -LiteralPath $apiDest)) {
    throw "Source Api folder not found: $apiDest"
}

Write-Host "Sync Api: $apiSrc -> $apiDest" -ForegroundColor Cyan
Write-Host "Skip: EasyManufacture.Api.csproj, LibRefs.props, LibRuntime.props" -ForegroundColor DarkGray

$robocopyArgs = @(
    $apiSrc,
    $apiDest,
    '/E',
    '/XD', 'bin', 'obj', '.vs',
    '/XF', 'EasyManufacture.Api.csproj', 'EasyManufacture.LibRefs.props', 'EasyManufacture.LibRuntime.props', '*.user',
    '/NFL', '/NDL', '/NJH', '/NJS', '/nc', '/ns', '/np'
)
if ($WhatIf) { $robocopyArgs += '/L' }

& robocopy @robocopyArgs | Out-Host
if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $LASTEXITCODE" }

Write-Host "Done. Verify: dotnet build EasyManufacture.Net10.sln -c Release" -ForegroundColor Green
