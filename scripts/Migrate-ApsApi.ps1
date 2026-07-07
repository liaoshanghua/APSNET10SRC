# Generate EasyManufacture.Api/Legacy/APSAPIController.* from EasyManufacture.Web APSAPIController
$ErrorActionPreference = 'Stop'
$net10 = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path $net10 -Parent
$webApi = Join-Path $repoRoot 'EasyManufacture.Web\Controllers\APSAPIController.cs'
$legacyCore = Join-Path $net10 'src\EasyManufacture.Infrastructure\Legacy\ApsCoreEngine.LegacyCore.cs'
$outBusiness = Join-Path $net10 'src\EasyManufacture.Api\Legacy\APSAPIController.LegacyBusiness.cs'
$outOverride = Join-Path $net10 'src\EasyManufacture.Api\Legacy\APSAPIController.ApsDataOverride.cs'
$legacyApiOld = Join-Path $net10 'src\EasyManufacture.Api\Legacy\ApsCoreEngine.LegacyApi.cs'
$overrideOld = Join-Path $net10 'src\EasyManufacture.Api\Legacy\ApsCoreEngine.ApsDataOverride.cs'

if (-not (Test-Path $webApi)) { throw "Web APSAPIController not found: $webApi" }

Write-Host "Read: $webApi" -ForegroundColor Cyan
$text = Get-Content $webApi -Raw -Encoding UTF8

# APSData override -> separate partial
$overrideBlock = $null
if ($text -match '(?s)(public override string\s+APSData\(\)\s*\{.*?\n\s*return base\.APSData\(\);\s*\})') {
    $overrideBlock = $Matches[1]
    $text = $text -replace '(?s)\s*/// <summary>\s*/// 自定义的接口类型\s*/// </summary>.*?return base\.APSData\(\);\s*\}\s*', "`n"
    $text = $text -replace '(?s)public override string\s+APSData\(\)\s*\{.*?\n\s*return base\.APSData\(\);\s*\}\s*', "`n"
}

$text = $text -replace '(?ms)\s*public ActionResult Index\(\)\s*\{[^}]*\}\s*', "`n"
$text = $text -replace '(?ms)\s*protected override void Initialize\([^)]*\)\s*\{.*?\n\s*\}\s*', "`n"

$text = $text -replace 'namespace EasyManufacture\.Web\.Controllers', 'namespace EasyManufacture.Api.Controllers'
$text = $text -replace 'public partial class APSAPIController\s*:\s*APSCore', 'public partial class APSAPIController : ApsCoreEngine'

$text = $text -replace 'using EasyManufacture\.Core\.MvcControl;', ''
$text = $text -replace 'using EasyManufacture\.Core\.DataBase;', 'using EasyManufacture.Infrastructure.Legacy;'
$text = $text -replace 'using EasyManufacture\.Core\.SystemInterface\.K3Cloud;', 'using EasyManufacture.Infrastructure.SystemInterface.K3Cloud;'
$text = $text -replace 'using EasyManufacture\.Core\.SystemInterface\.SAP;', 'using EasyManufacture.Infrastructure.SystemInterface.SAP;'
$text = $text -replace 'using EasyManufacture\.Core;', 'using EasyManufacture.Infrastructure.Legacy;'
$text = $text -replace 'using System\.Web\.Mvc;', ''
$text = $text -replace 'using System\.Web;', ''
$text = $text -replace 'using System\.Data\.Entity;', ''
$text = $text -replace 'using System\.Data\.SqlClient;', 'using Microsoft.Data.SqlClient;'
$text = $text -replace 'using static EasyManufacture\.Core\.SystemInterface\.K3Cloud\.K3', 'using static EasyManufacture.Infrastructure.SystemInterface.K3Cloud.K3'
$text = $text -replace '\bbase\.setRowDetail\b', 'setRowDetail'
$text = $text -replace '\bbase\.setDt\b', 'setDt'
$text = $text -replace '\bbase\.setWhere\b', 'setWhere'
$text = $text -replace '\bbase\.setAfterReadRow\b', 'setAfterReadRow'
$text = $text -replace 'System\.Data\.SqlClient', 'Microsoft.Data.SqlClient'
$text = $text -replace 'JsonResult', 'ApsLegacyJsonResult'
$text = $text -replace '\(HttpContext\?\.Connection\.RemoteIpAddress\?\.ToString\(\) \?\? ""\)', 'Request.UserHostAddress'
$text = $text -replace 'ConfigurationManager\.AppSettings', 'System.Configuration.ConfigurationManager.AppSettings'
$text = $text -replace 'HttpUtility\.UrlEncode', 'Uri.EscapeDataString'
$text = $text -replace '\[HttpPost\]\s*\r?\n\s*\[HttpPost\]', '[HttpPost]'
$text = $text -replace '(?m)^\s*\[HttpPost\]\s*\r?\n', ''

# 基类 ApsCoreEngine 已有字段，子类不再重复声明
$text = $text -replace '(?ms)\s*DataTable\s+DtDetail\s*=\s*null;\s*', "`n"
$text = $text -replace '(?ms)\s*DataSet\s+DsDetail\s*=\s*null;\s*', "`n"
$text = $text -replace '(?ms)\s*DataTable\s+DtDetail1\s*=\s*null;\s*', "`n"
$text = $text -replace '(?ms)\s*DataTable\s+DtDetail2\s*=\s*null;\s*', "`n"

# 扫描 LegacyCore 公共方法，对同名方法加 new 隐藏基类
$baseText = Get-Content $legacyCore -Raw -Encoding UTF8
$baseMethodRx = [regex]'(?m)^\s+public\s+(?:virtual\s+)?(?:override\s+)?(?:async\s+)?(?:Task<(?:IActionResult|ActionResult|string)>|(?:IActionResult|ActionResult|string|void|int|bool|decimal|double|float|DataTable|DataSet|JObject|JArray))\s+(?<name>\w+)\s*\('
$baseNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($m in $baseMethodRx.Matches($baseText)) { [void]$baseNames.Add($m.Groups['name'].Value) }

$methodRx = [regex]'(?m)^(\s+)public\s+(?!new\s)(?<sig>(?:async\s+)?(?:override\s+)?(?:Task<[^>]+>|IActionResult|ActionResult|string|void|int|bool|decimal|double|float|DataTable|DataSet|JObject|JArray)\s+(?<name>\w+)\s*\([^)]*\))'
$text = $methodRx.Replace($text, {
    param($m)
    $name = $m.Groups['name'].Value
    if ($baseNames.Contains($name)) {
        return "$($m.Groups[1].Value)public new $($m.Groups['sig'].Value)"
    }
    return $m.Value
})

$header = @'
// <auto-generated>
// 旧 Web APSAPIController 业务方法 — 源码归 EasyManufacture.Api/Legacy（NuGet 引用版可见）。
// 重新生成: scripts/Migrate-ApsApi.ps1
// </auto-generated>
using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Infrastructure.SystemInterface.K3Cloud;
using EasyManufacture.Infrastructure.SystemInterface.SAP;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static EasyManufacture.Infrastructure.SystemInterface.K3Cloud.K3;
using ApsLegacyJsonResult = EasyManufacture.Infrastructure.Legacy.ApsLegacyJsonResult;

'@

$text = $text -replace '(?s)^using .+?namespace EasyManufacture\.Api\.Controllers;', 'namespace EasyManufacture.Api.Controllers;'
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outBusiness, ($header + $text.TrimStart()), $utf8)
Write-Host "Wrote $outBusiness ($((Get-Item $outBusiness).Length) bytes)" -ForegroundColor Green

if ($overrideBlock) {
    $ob = $overrideBlock -replace 'public override string\s+APSData\(\)', 'public string RunLegacyApsDataWithDicHooks()'
    $ob = $ob -replace 'return base\.APSData\(\)', 'return base.APSData()'
    $ob = $ob -replace '\bbase\.setRowDetail\b', 'setRowDetail'
    $ob = $ob -replace '\bbase\.setDt\b', 'setDt'
    $ob = $ob -replace '\bbase\.setWhere\b', 'setWhere'
    $ob = $ob -replace '\bbase\.setAfterReadRow\b', 'setAfterReadRow'
    $overrideFile = @"
// APSAPIController.override APSData() dic switch
using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Api.Controllers;

public partial class APSAPIController
{
    $ob
}
"@
    [System.IO.File]::WriteAllText($outOverride, $overrideFile, $utf8)
    Write-Host "Wrote $outOverride" -ForegroundColor Green
}

if (Test-Path $legacyApiOld) { Remove-Item $legacyApiOld -Force; Write-Host "Removed old $legacyApiOld" -ForegroundColor Yellow }
if (Test-Path $overrideOld) { Remove-Item $overrideOld -Force; Write-Host "Removed old $overrideOld" -ForegroundColor Yellow }
