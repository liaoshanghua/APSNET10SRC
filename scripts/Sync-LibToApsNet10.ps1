# 源码版业务 DLL → 公版 lib/（规则：Infrastructure 等在源码版改，编译后同步 DLL 到公版）

param(
    [string]$ApsNet10Root = '',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipBuild,
    [string]$ObfuscatedLibDir = ''
)

$ErrorActionPreference = 'Stop'
$net10 = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($ApsNet10Root)) {
    $ApsNet10Root = Join-Path $net10 '..\APSNET10'
}
$ApsNet10Root = [IO.Path]::GetFullPath($ApsNet10Root)

$sync = Join-Path $ApsNet10Root 'scripts\Sync-LibFromNet10.ps1'
if (-not (Test-Path -LiteralPath $sync)) {
    throw "未找到 $sync"
}

& $sync -Net10Root $net10 -Configuration $Configuration -SkipBuild:$SkipBuild -ObfuscatedLibDir $ObfuscatedLibDir
