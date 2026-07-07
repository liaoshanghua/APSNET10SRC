#Requires -Version 5.1
<#
.SYNOPSIS
  发布 APS 并将包同步到目标目录的 update\，可选在目标机执行热更新。

.DESCRIPTION
  1. dotnet publish（调用 Publish-Aps.ps1）
  2. 将发布结果复制到 <TargetPath>\update\
  3. 若 -RunHotUpdate 且 TargetPath 为本机可访问路径，执行 Apply-ApsHotUpdate.ps1

.EXAMPLE
  # 开发机：只发布并同步到服务器共享的 update 文件夹（需在服务器上再双击 APS-热更新.bat）
  .\Publish-Aps-HotDeploy.ps1 -TargetPath "\\192.168.1.88\APSNEW"

.EXAMPLE
  # 在服务器本机执行：发布到 D:\APSNEW\update 并自动热更新
  .\Publish-Aps-HotDeploy.ps1 -TargetPath "D:\APSNEW" -RunHotUpdate -Confirm

.EXAMPLE
  # 使用已有 publish 目录，只同步并热更新
  .\Publish-Aps-HotDeploy.ps1 -TargetPath "D:\APSNEW" -SkipPublish -RunHotUpdate -Confirm
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $TargetPath,

    [string] $PublishOutput = (Join-Path $PSScriptRoot '..\publish\api'),

    [switch] $SkipPublish,

    [switch] $RunHotUpdate,

    [switch] $Backup,

    [switch] $NoStart,

    [switch] $WithRuntime,

    [switch] $CompressWwwroot,

    [switch] $Confirm
)

$ErrorActionPreference = 'Stop'

$PublishOutput = [System.IO.Path]::GetFullPath($PublishOutput)
$TargetPath = $TargetPath.Trim().TrimEnd('\', '/')
$updateDir = Join-Path $TargetPath 'update'

if (-not $SkipPublish) {
    $publishArgs = @(
        '-OutputDir', $PublishOutput
    )
    if ($WithRuntime) { $publishArgs += '-WithRuntime' }
    if ($CompressWwwroot) { $publishArgs += '-CompressWwwroot' }

    Write-Host ">>> 发布 APS -> $PublishOutput" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'Publish-Aps.ps1') @publishArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
else {
    if (-not (Test-Path -LiteralPath $PublishOutput)) {
        throw "SkipPublish 时 PublishOutput 必须存在: $PublishOutput"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $PublishOutput 'APS.exe')) -and
    -not (Test-Path -LiteralPath (Join-Path $PublishOutput 'APS.dll'))) {
    throw "发布目录无效（无 APS.exe / APS.dll）: $PublishOutput"
}

Write-Host ">>> 同步到 $updateDir" -ForegroundColor Cyan
if (-not (Test-Path -LiteralPath $TargetPath)) {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $updateDir)) {
    New-Item -ItemType Directory -Path $updateDir -Force | Out-Null
}

$robocopyArgs = @(
    $PublishOutput,
    $updateDir,
    '/MIR',
    '/R:3',
    '/W:5',
    '/NFL',
    '/NDL',
    '/NP'
)
& robocopy @robocopyArgs | Out-Host
$rc = $LASTEXITCODE
if ($rc -ge 8) {
    throw "同步到 update 失败，robocopy 退出码 $rc"
}

Write-Host "已同步到: $updateDir" -ForegroundColor Green

if (-not $RunHotUpdate) {
    Write-Host ''
    Write-Host '下一步：在服务器上进入 APS 安装目录，运行 APS-热更新.bat' -ForegroundColor Yellow
    Write-Host "  或: powershell -File `"$TargetPath\Apply-ApsHotUpdate.ps1`" -Confirm"
    exit 0
}

$applyScript = Join-Path $TargetPath 'Apply-ApsHotUpdate.ps1'
if (-not (Test-Path -LiteralPath $applyScript)) {
    $srcApply = Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\Deployment\Apply-ApsHotUpdate.ps1'
    if (Test-Path -LiteralPath $srcApply) {
        Copy-Item -LiteralPath $srcApply -Destination $applyScript -Force
        Write-Host "已复制 Apply-ApsHotUpdate.ps1 到 $TargetPath"
    }
    else {
        throw "目标目录缺少 Apply-ApsHotUpdate.ps1: $applyScript"
    }
}

Write-Host ">>> 执行热更新: $TargetPath" -ForegroundColor Cyan
$hotArgs = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $applyScript,
    '-InstallDir', $TargetPath,
    '-UpdateSource', $updateDir
)
if ($Backup) { $hotArgs += '-Backup' }
if ($NoStart) { $hotArgs += '-NoStart' }
if ($Confirm) { $hotArgs += '-Confirm' }

& powershell @hotArgs
exit $LASTEXITCODE
