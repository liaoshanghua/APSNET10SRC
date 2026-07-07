# 开发机/发布目录：注册 APS 系统启动自启（转发到 Deployment 脚本）
param(
    [string]$PublishPath = (Join-Path $PSScriptRoot '..\publish\api'),
    [string]$TaskName = 'APS',
    [int]$Port = 0
)

$script = Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\Deployment\Install-ApsAutoStart.ps1'
if (-not (Test-Path $script)) {
    $script = Join-Path $PublishPath 'Install-ApsAutoStart.ps1'
}
if (-not (Test-Path $script)) {
    Write-Error "找不到 Install-ApsAutoStart.ps1"
}

& $script -PublishPath $PublishPath -TaskName $TaskName -Port $Port
