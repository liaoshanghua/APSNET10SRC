# 转发到 Api 项目 Deployment 目录（源码唯一副本，发布时自动复制到输出目录）
$canonical = Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\Deployment\Install-ApsDependencies.ps1'
if (-not (Test-Path $canonical)) {
    Write-Error "Not found: $canonical"
}
& $canonical @args
exit $LASTEXITCODE
