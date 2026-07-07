# 将旧 EasyManufacture.Entitys 中视图/表实体复制到 Net10（供 APSCore 全量编译）
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$srcDir = Join-Path $root 'EasyManufacture.Entitys'
$dstDir = Join-Path $PSScriptRoot '..\src\EasyManufacture.Infrastructure\Legacy\Entities'

if (-not (Test-Path $srcDir)) { throw "找不到 $srcDir" }
if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Path $dstDir -Force | Out-Null }

$patterns = @('V_*.cs', 'APS_*.cs', 'Dev_*.cs', 'IM_*.cs')
$copied = 0
foreach ($pat in $patterns) {
    Get-ChildItem $srcDir -Filter $pat -File | ForEach-Object {
        $dest = Join-Path $dstDir $_.Name
        if (-not (Test-Path $dest)) {
            Copy-Item $_.FullName $dest -Force
            $copied++
        }
    }
}
$exDir = Join-Path $srcDir 'Ex'
if (Test-Path $exDir) {
    $exDst = Join-Path $dstDir 'Ex'
    if (-not (Test-Path $exDst)) { New-Item -ItemType Directory -Path $exDst -Force | Out-Null }
    Get-ChildItem $exDir -Filter '*.cs' -File | ForEach-Object {
        $dest = Join-Path $exDst $_.Name
        Copy-Item $_.FullName $dest -Force
        $copied++
    }
}
Write-Host "实体桩复制完成，新增 $copied 个文件，目录: $dstDir"
