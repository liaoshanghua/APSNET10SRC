# 为 Deployment / 发布目录中的 .bat、.ps1 写入 UTF-8 BOM，避免 CMD 中文乱码
param(
    [Parameter(Mandatory = $true)]
    [string] $TargetDir
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $TargetDir)) { return }

$utf8Bom = New-Object System.Text.UTF8Encoding $true
$count = 0

Get-ChildItem -LiteralPath $TargetDir -File -Include '*.bat', '*.ps1' -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object {
        $text = [System.IO.File]::ReadAllText($_.FullName)
        if ($text.Length -gt 0 -and [int][char]$text[0] -eq 0xFEFF) { return }
        [System.IO.File]::WriteAllText($_.FullName, $text.TrimStart([char]0xFEFF), $utf8Bom)
        $count++
    }

if ($count -gt 0) {
    Write-Host "UTF-8 BOM: $count file(s) in $TargetDir"
}
