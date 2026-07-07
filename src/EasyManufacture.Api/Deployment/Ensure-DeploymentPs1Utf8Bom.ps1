# Windows PowerShell 5.1 无 BOM 的 UTF-8 脚本会把中文误读为 GBK，导致字符串未闭合。
# 发布/拷贝部署脚本后执行本脚本，为 Deployment 目录下所有 .ps1 写入 UTF-8 BOM。
param(
    [string]$TargetDir = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
$utf8Bom = New-Object System.Text.UTF8Encoding $true

Get-ChildItem -LiteralPath $TargetDir -Filter '*.ps1' -File | ForEach-Object {
    if ($_.Name -eq 'Ensure-DeploymentPs1Utf8Bom.ps1') { return }
    $text = [System.IO.File]::ReadAllText($_.FullName)
    [System.IO.File]::WriteAllText($_.FullName, $text, $utf8Bom)
    Write-Host "UTF-8 BOM: $($_.Name)"
}
