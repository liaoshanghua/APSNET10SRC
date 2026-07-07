#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $PSScriptRoot 'docx-template-yingruifeng'
$outDir = Join-Path $root 'docs'
$outFileEn = Join-Path $outDir 'YingRuifeng-APS-DayPlan-Rules.docx'
$outFile = Join-Path $outDir '盈瑞丰APS滚动日计划规则说明.docx'

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
if (Test-Path $outFileEn) { Remove-Item $outFileEn -Force }

$zip = [System.IO.Compression.ZipFile]::Open($outFileEn, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -LiteralPath $srcDir -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($srcDir.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $rel) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

if (Test-Path -LiteralPath $outFile) { Remove-Item -LiteralPath $outFile -Force }
[System.IO.File]::Copy($outFileEn, $outFile, $true)
Write-Host "Generated: $outFileEn"
Write-Host "Generated: $outFile"
