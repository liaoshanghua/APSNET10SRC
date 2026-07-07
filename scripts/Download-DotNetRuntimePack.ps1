# 预下载 .NET 10 运行时安装包到发布目录（离线部署用）
# 用法：发布完成后执行，将 runtime/ 文件夹与 APS 一起拷贝到服务器
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\publish\api\runtime'),

    [string]$Channel = '10.0',

    [ValidateSet('x64', 'x86', 'arm64')]
    [string]$Arch = 'x64',

    [switch]$IncludeDotNetInstallScript
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}
$OutputDir = (Resolve-Path $OutputDir).Path

Write-Host "Output: $OutputDir"
Write-Host "Fetching .NET $Channel release index ..."

$indexUrl = "https://dotnetcli.azureedge.net/dotnet/release-index/$Channel/releases.json"
$index = Invoke-RestMethod -Uri $indexUrl -UseBasicParsing

$release = $index.releases |
    Where-Object { $_.sdk.version -match "^$([regex]::Escape($Channel))" -or $_.aspnetcoreruntime.version -match "^$([regex]::Escape($Channel))" } |
    Select-Object -First 1

if (-not $release) {
    $release = $index.releases | Select-Object -First 1
}

if (-not $release) {
    throw "No release found for channel $Channel"
}

function Save-RuntimeFile {
    param(
        [object]$FileEntry,
        [string]$Kind
    )

    if (-not $FileEntry) { return }

    $url = $FileEntry.url
    if ([string]::IsNullOrWhiteSpace($url)) { return }

    $name = $FileEntry.name
    if ([string]::IsNullOrWhiteSpace($name)) {
        $name = Split-Path $url -Leaf
    }

    $dest = Join-Path $OutputDir $name
    if (Test-Path $dest) {
        Write-Host "  skip (exists): $name" -ForegroundColor DarkGray
        return
    }

    Write-Host "  downloading $Kind : $name ..."
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
    Write-Host "  saved: $dest" -ForegroundColor Green
}

$archKey = switch ($Arch) {
    'x64' { 'x64' }
    'x86' { 'x86' }
    'arm64' { 'arm64' }
}

# releases.json 结构：runtime/dotnet / runtime/aspnetcore 下按架构列出文件
$dotnetFiles = $release.runtime.dotnet.files
$aspnetFiles = $release.runtime.aspnetcore.files

$dotnetExe = $dotnetFiles | Where-Object { $_.rid -eq "win-$archKey" -and $_.name -like 'dotnet-runtime-*-win-*.exe' } | Select-Object -First 1
$aspnetExe = $aspnetFiles | Where-Object { $_.rid -eq "win-$archKey" -and $_.name -like 'aspnetcore-runtime-*-win-*.exe' } | Select-Object -First 1
$desktopFiles = $release.runtime.windowsdesktop.files
$desktopExe = $desktopFiles | Where-Object { $_.rid -eq "win-$archKey" -and $_.name -like 'windowsdesktop-runtime-*-win-*.exe' } | Select-Object -First 1

if (-not $dotnetExe -and $dotnetFiles) {
    $dotnetExe = $dotnetFiles | Where-Object { $_.name -like "*dotnet-runtime*win-$archKey*.exe" } | Select-Object -First 1
}
if (-not $aspnetExe -and $aspnetFiles) {
    $aspnetExe = $aspnetFiles | Where-Object { $_.name -like "*aspnetcore-runtime*win-$archKey*.exe" } | Select-Object -First 1
}

if (-not $desktopExe -and $desktopFiles) {
    $desktopExe = $desktopFiles | Where-Object { $_.name -like "*windowsdesktop-runtime*win-$archKey*.exe" } | Select-Object -First 1
}

Save-RuntimeFile -FileEntry $dotnetExe -Kind '.NET Runtime'
Save-RuntimeFile -FileEntry $aspnetExe -Kind 'ASP.NET Core Runtime'
Save-RuntimeFile -FileEntry $desktopExe -Kind 'Windows Desktop Runtime'

if ($IncludeDotNetInstallScript) {
    $scriptDest = Join-Path $OutputDir 'dotnet-install.ps1'
    if (-not (Test-Path $scriptDest)) {
        Write-Host '  downloading dotnet-install.ps1 ...'
        Invoke-WebRequest `
            -Uri 'https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1' `
            -OutFile $scriptDest `
            -UseBasicParsing
        Write-Host "  saved: $scriptDest" -ForegroundColor Green
    }
}

Write-Host ''
Write-Host 'Done. Copy the whole publish folder (including runtime/) to the server.' -ForegroundColor Cyan
Write-Host 'Install order on server: APS-启动.bat -> uses runtime/*.exe offline first.'
