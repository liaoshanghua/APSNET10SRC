#Requires -Version 5.1
<#
.SYNOPSIS
  还原成功后 TRUNCATE 指定前缀的 APS 业务表（dbo 下 name LIKE 前缀+'%'）。

.DESCRIPTION
  默认匹配表名前缀：
    APS_Order*, APS_Material*, APS_PO*,
    APS_ProcessMaterial*, APS_ProcessPlan*, APS_SalesOrder*

  还原前会将目标库设为 SINGLE_USER（ROLLBACK IMMEDIATE），请确保已停止 APS 服务。

.EXAMPLE
  # 从 appsettings 读连接串，还原并清空核心表
  .\Restore-ApsDatabase.ps1 -BackupPath "D:\Backup\APS20260301.bak" -Confirm

.EXAMPLE
  # 仅清空核心表（不还原）
  .\Restore-ApsDatabase.ps1 -SkipRestore -Confirm

.EXAMPLE
  # 指定服务器与库名
  .\Restore-ApsDatabase.ps1 -BackupPath "D:\Backup\APS.bak" -Server "192.168.1.88" -Database "APS20260602" -SqlUser sa -SqlPassword "***" -Confirm
#>
param(
    [string] $BackupPath,

    [string] $Server,

    [string] $Database,

    [string] $SqlUser,

    [string] $SqlPassword,

    [string] $ConnectionString = $env:APS_CONNECTION_STRING,

    [string] $AppSettingsPath = '',

    [string] $TruncateProcScript = '',

    [switch] $SkipRestore,

    [switch] $SkipTruncate,

    [switch] $Confirm
)

$ErrorActionPreference = 'Stop'

$tablePrefixes = @(
    'APS_Order',
    'APS_Material',
    'APS_PO',
    'APS_ProcessMaterial',
    'APS_ProcessPlan',
    'APS_SalesOrder'
)

if ([string]::IsNullOrWhiteSpace($AppSettingsPath)) {
    $candidates = @(
        (Join-Path $PSScriptRoot 'appsettings.json'),
        (Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\appsettings.json')
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { $AppSettingsPath = $c; break }
    }
    if ([string]::IsNullOrWhiteSpace($AppSettingsPath)) {
        $AppSettingsPath = $candidates[-1]
    }
}

if ([string]::IsNullOrWhiteSpace($TruncateProcScript)) {
    $candidates = @(
        (Join-Path $PSScriptRoot 'P_APS_TruncateCoreTablesAfterRestore.sql'),
        (Join-Path $PSScriptRoot '..\docs\sql\P_APS_TruncateCoreTablesAfterRestore.sql')
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { $TruncateProcScript = $c; break }
    }
    if ([string]::IsNullOrWhiteSpace($TruncateProcScript)) {
        $TruncateProcScript = $candidates[-1]
    }
}

function Resolve-SqlCmdPath {
    $candidates = @(
        'sqlcmd',
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe"
    )
    foreach ($p in $candidates) {
        if ($p -eq 'sqlcmd') {
            $found = Get-Command sqlcmd -ErrorAction SilentlyContinue
            if ($found) { return $found.Source }
            continue
        }
        if (Test-Path -LiteralPath $p) { return $p }
    }
    throw '未找到 sqlcmd。请安装 SQL Server 命令行工具。'
}

function Parse-ConnectionString {
    param([string] $Cs)

    if ([string]::IsNullOrWhiteSpace($Cs)) {
        throw '未提供连接串。请设置环境变量 APS_CONNECTION_STRING 或参数 ConnectionString。'
    }

    $map = @{}
    foreach ($part in ($Cs -split ';')) {
        if ([string]::IsNullOrWhiteSpace($part)) { continue }
        $eq = $part.IndexOf('=')
        if ($eq -lt 1) { continue }
        $key = $part.Substring(0, $eq).Trim()
        $val = $part.Substring($eq + 1).Trim()
        $map[$key.ToLowerInvariant()] = $val
    }

    $server = $map['data source']
    if (-not $server) { $server = $map['server'] }
    $database = $map['initial catalog']
    if (-not $database) { $database = $map['database'] }
    $user = $map['user id']
    if (-not $user) { $user = $map['user'] }
    $password = $map['password']
    $trusted = ($map['integrated security'] -match '^(true|sspi)$' -or $map['trusted_connection'] -match '^(true|yes)$')

    if (-not $server -or -not $database) {
        throw "连接串无法解析服务器或数据库名: $Cs"
    }

    return [pscustomobject]@{
        Server             = $server
        Database           = $database
        User               = $user
        Password           = $password
        IntegratedSecurity = [bool]$trusted
    }
}

function Get-ConnectionFromAppSettings {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    return $json.ConnectionStrings.MSSQLConnectionString
}

function Invoke-SqlCmdText {
    param(
        [string] $SqlCmd,
        [string] $ServerName,
        [string] $DatabaseName,
        [string] $UserName,
        [string] $PasswordPlain,
        [switch] $IntegratedSecurity,
        [int] $QueryTimeout = 0
    )

    $args = @(
        '-S', $ServerName,
        '-d', $DatabaseName,
        '-b',
        '-V', '16'
    )
    if ($QueryTimeout -gt 0) {
        $args += @('-t', "$QueryTimeout")
    }
    if ($IntegratedSecurity) {
        $args += '-E'
    }
    else {
        if ([string]::IsNullOrWhiteSpace($UserName)) {
            throw '需要 SQL 登录名：连接串中无 user，或未指定 -SqlUser。'
        }
        $args += @('-U', $UserName, '-P', $PasswordPlain)
    }
    $args += @('-Q', $Sql)

    & $SqlCmd @args
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd 执行失败，退出码 $LASTEXITCODE"
    }
}

function Invoke-SqlCmdFile {
    param(
        [string] $SqlCmd,
        [string] $ServerName,
        [string] $DatabaseName,
        [string] $UserName,
        [string] $PasswordPlain,
        [switch] $IntegratedSecurity,
        [string] $InputFile
    )

    $args = @(
        '-S', $ServerName,
        '-d', $DatabaseName,
        '-b',
        '-V', '16',
        '-i', $InputFile
    )
    if ($IntegratedSecurity) {
        $args += '-E'
    }
    else {
        $args += @('-U', $UserName, '-P', $PasswordPlain)
    }

    & $SqlCmd @args
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd 执行脚本失败: $InputFile"
    }
}

if (-not $SkipRestore -and [string]::IsNullOrWhiteSpace($BackupPath)) {
    throw '请指定 -BackupPath，或使用 -SkipRestore 仅清空核心表。'
}

if (-not $SkipRestore) {
    $BackupPath = [System.IO.Path]::GetFullPath($BackupPath)
    if (-not (Test-Path -LiteralPath $BackupPath)) {
        throw "备份文件不存在: $BackupPath"
    }
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = Get-ConnectionFromAppSettings -Path $AppSettingsPath
}

$conn = Parse-ConnectionString -Cs $ConnectionString

if ($Server) { $conn.Server = $Server }
if ($Database) { $conn.Database = $Database }
if ($SqlUser) { $conn.User = $SqlUser }
if ($SqlPassword) { $conn.Password = $SqlPassword }

$sqlcmd = Resolve-SqlCmdPath

Write-Host ''
Write-Host '========== APS 数据库还原 / 清空核心表 ==========' -ForegroundColor Cyan
Write-Host ("服务器   : {0}" -f $conn.Server)
Write-Host ("数据库   : {0}" -f $conn.Database)
if (-not $SkipRestore) {
    Write-Host ("备份文件 : {0}" -f $BackupPath)
}
Write-Host ("表前缀   : {0}" -f (($tablePrefixes | ForEach-Object { "$_*" }) -join ', '))
Write-Host '================================================' -ForegroundColor Cyan
Write-Host ''

if (-not $Confirm) {
    Write-Host '此操作会覆盖目标库数据，且不可撤销。' -ForegroundColor Yellow
    $answer = Read-Host '请输入 YES 继续'
    if ($answer -ne 'YES') {
        Write-Host '已取消。'
        exit 0
    }
}

$authArgs = @{
    ServerName         = $conn.Server
    UserName           = $conn.User
    PasswordPlain      = $conn.Password
    IntegratedSecurity = [switch]$conn.IntegratedSecurity
}

if (-not $SkipRestore) {
    $bakForSql = $BackupPath.Replace("'", "''")
    $dbForSql = $conn.Database.Replace(']', ']]')

    Write-Host '[1/3] 还原数据库...' -ForegroundColor Green

    $restoreSql = @"
ALTER DATABASE [$dbForSql] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$dbForSql]
FROM DISK = N'$bakForSql'
WITH REPLACE, RECOVERY, STATS = 10;
ALTER DATABASE [$dbForSql] SET MULTI_USER;
"@

    Invoke-SqlCmdText -SqlCmd $sqlcmd -DatabaseName 'master' -QueryTimeout 0 @authArgs -Sql $restoreSql
    Write-Host '      还原完成。' -ForegroundColor Green
}
else {
    Write-Host '[1/3] 跳过还原 (-SkipRestore)。' -ForegroundColor DarkGray
}

if (-not $SkipTruncate) {
    Write-Host '[2/3] 部署/更新清空存储过程...' -ForegroundColor Green
    if (-not (Test-Path -LiteralPath $TruncateProcScript)) {
        throw "找不到 SQL 脚本: $TruncateProcScript"
    }
    Invoke-SqlCmdFile -SqlCmd $sqlcmd -DatabaseName $conn.Database @authArgs -InputFile $TruncateProcScript

    Write-Host '[3/3] 清空匹配前缀的 APS 表...' -ForegroundColor Green
    Invoke-SqlCmdText -SqlCmd $sqlcmd -DatabaseName $conn.Database @authArgs -Sql 'EXEC dbo.P_APS_TruncateCoreTablesAfterRestore;'
    Write-Host '      匹配前缀的表已 TRUNCATE（详见 sqlcmd 输出的表名列表）。' -ForegroundColor Green
}
else {
    Write-Host '[2/3] 跳过清空表 (-SkipTruncate)。' -ForegroundColor DarkGray
    Write-Host '[3/3] 完成。' -ForegroundColor DarkGray
}

Write-Host ''
Write-Host '全部完成。请重启 APS 服务后再使用系统。' -ForegroundColor Cyan
