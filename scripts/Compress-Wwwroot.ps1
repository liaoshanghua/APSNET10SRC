# 为 wwwroot 静态资源生成 .br / .gz（大 JS/CSS 部署前预压缩，配合 APS 预压缩中间件）
param(
    [Parameter(Mandatory = $true)]
    [string]$WwwRoot,

    [ValidateSet('Fastest', 'Optimal', 'SmallestSize')]
    [string]$Level = 'Optimal'
)

$ErrorActionPreference = 'Stop'
$WwwRoot = (Resolve-Path $WwwRoot).Path

$compressionLevel = [System.IO.Compression.CompressionLevel]::$Level
$patterns = @('*.js', '*.css', '*.json', '*.html', '*.htm', '*.svg', '*.wasm', '*.map')
$files = foreach ($p in $patterns) {
    Get-ChildItem -Path $WwwRoot -Filter $p -Recurse -File -ErrorAction SilentlyContinue
}

if (-not $files) {
    Write-Host "No static files under $WwwRoot"
    exit 0
}

function Write-BrotliFile {
    param([byte[]]$Bytes, [string]$Dest)
    $input = New-Object System.IO.MemoryStream(,$Bytes)
    $output = [System.IO.File]::Create($Dest)
    try {
        $brotli = New-Object System.IO.Compression.BrotliStream($output, $compressionLevel, $true)
        try { $input.CopyTo($brotli) } finally { $brotli.Dispose() }
    }
    finally {
        $output.Dispose()
        $input.Dispose()
    }
}

function Write-GzipFile {
    param([byte[]]$Bytes, [string]$Dest)
    $input = New-Object System.IO.MemoryStream(,$Bytes)
    $output = [System.IO.File]::Create($Dest)
    try {
        $gzip = New-Object System.IO.Compression.GZipStream($output, $compressionLevel, $true)
        try { $input.CopyTo($gzip) } finally { $gzip.Dispose() }
    }
    finally {
        $output.Dispose()
        $input.Dispose()
    }
}

$count = 0
foreach ($file in $files) {
    if ($file.Extension -in @('.br', '.gz')) { continue }

    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $br = "$($file.FullName).br"
    $gz = "$($file.FullName).gz"

    Write-BrotliFile $bytes $br
    Write-GzipFile $bytes $gz
    $brLen = (Get-Item $br).Length
    $ratio = [math]::Round(100.0 * $brLen / $bytes.Length, 1)
    Write-Host ("  {0} -> .br ({1}%, {2:N0} -> {3:N0} bytes)" -f $file.Name, $ratio, $bytes.Length, $brLen)
    $count++
}

Write-Host "Compressed $count files under $WwwRoot" -ForegroundColor Green
