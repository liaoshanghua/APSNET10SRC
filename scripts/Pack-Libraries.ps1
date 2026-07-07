<#
.SYNOPSIS
  将 EasyManufacture.Net10 类库打包到本地 NuGet 源 packages/。

.DESCRIPTION
  按依赖顺序 pack：Domain → Application → Licence → Infrastructure。
  Infrastructure 包内含 SAP / 金蝶原生 DLL。
  打包完成后，在 EasyManufacture.Api 目录执行 dotnet restore / build 即可引用最新包。

.PARAMETER Configuration
  构建配置，默认 Release。

.EXAMPLE
  .\scripts\Pack-Libraries.ps1
  .\scripts\Pack-Libraries.ps1 -Configuration Debug
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

# 必须按依赖顺序 pack，Infrastructure 最后（依赖其余三个包）
$projects = @(
    'src\EasyManufacture.Domain\EasyManufacture.Domain.csproj',
    'src\EasyManufacture.Application\EasyManufacture.Application.csproj',
    'src\EasyManufacture.Licence\EasyManufacture.Licence.csproj',
    'src\EasyManufacture.Infrastructure\EasyManufacture.Infrastructure.csproj'
)

Push-Location $root
try {
    New-Item -ItemType Directory -Force -Path 'packages' | Out-Null

    Write-Host "==> dotnet build -c $Configuration" -ForegroundColor Cyan
    dotnet build EasyManufacture.Net10.sln -c $Configuration --no-restore 2>$null
    if ($LASTEXITCODE -ne 0) {
        dotnet build EasyManufacture.Net10.sln -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    }

    foreach ($proj in $projects) {
        Write-Host "==> dotnet pack $proj" -ForegroundColor Cyan
        dotnet pack $proj -c $Configuration --no-build -o packages
        if ($LASTEXITCODE -ne 0) { throw "Pack failed: $proj" }
    }

    Write-Host ""
    Write-Host "已输出到: $root\packages" -ForegroundColor Green
    Write-Host "请在 EasyManufacture.Api 中 dotnet restore 后重新构建。" -ForegroundColor Yellow
    Get-ChildItem packages -Filter '*.nupkg' | Sort-Object Name | ForEach-Object { Write-Host "  $($_.Name)" }
}
finally {
    Pop-Location
}
