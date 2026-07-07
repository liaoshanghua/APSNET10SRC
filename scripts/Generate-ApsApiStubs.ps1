# 根据 ApsCoreEngine 全量源码生成 APSAPIController 缺失路由 stub
# 输出：src/EasyManufacture.Api/Controllers/APSAPIController.LegacyCoreStubs.cs
# 编码：UTF-8（避免中文 summary 乱码）
$ErrorActionPreference = 'Stop'
$infra = Join-Path $PSScriptRoot '..\src\EasyManufacture.Infrastructure\Legacy'
$ctrlDir = Join-Path $PSScriptRoot '..\src\EasyManufacture.Api\Controllers'
$outFile = Join-Path $ctrlDir 'APSAPIController.LegacyCoreStubs.cs'

$excludeName = '^(Index|Initialize|GetConfigForObj|GetJspreadsheetConfigObj|Translation|ExportExcel|ExportExcelOLD|OutputClient|SetDataColor|AbstractXSSFChartSerie|allMsg)$'
$excludePrefix = '^(SetDt|setDetail|setDetai|setAfterReadRow|SetWhere)'

$engineFiles = Get-ChildItem (Join-Path $infra 'ApsCoreEngine*.cs') -File |
    Where-Object { $_.Name -notmatch 'LegacySharedState|DependencyInjection|LegacyMvcCompat' }

$methodRx = [regex]'(?m)^\s+public\s+(?:async\s+)?(?:virtual\s+)?(?:override\s+)?(?:Task<(?:IActionResult|ActionResult|JsonResult|ContentResult|string)>\s+|(?:IActionResult|ActionResult|JsonResult|ContentResult|string)\s+)(?<name>\w+)\s*\((?<params>[^)]*)\)'

$engineMethods = @{}
foreach ($file in $engineFiles) {
    $text = Get-Content $file.FullName -Raw -Encoding UTF8
    foreach ($m in $methodRx.Matches($text)) {
        $name = $m.Groups['name'].Value
        if ($name -match $excludeName -or $name -match $excludePrefix) { continue }
        $params = $m.Groups['params'].Value.Trim()
        if ($params -match '\bref\b|\bout\b') { continue }
        if (-not $engineMethods.ContainsKey($name)) {
            $engineMethods[$name] = $params
        }
    }
}

$ctrlMethods = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
Get-ChildItem (Join-Path $ctrlDir 'APSAPIController*.cs') -File |
    Where-Object { $_.Name -ne 'APSAPIController.LegacyCoreStubs.cs' } |
    ForEach-Object {
        $text = Get-Content $_.FullName -Raw -Encoding UTF8
        foreach ($m in [regex]::Matches($text, 'public\s+(?:Task<(?:IActionResult|string)>|IActionResult|string)\s+(\w+)\s*\(')) {
            [void]$ctrlMethods.Add($m.Groups[1].Value)
        }
    }

$missing = $engineMethods.Keys | Where-Object { -not $ctrlMethods.Contains($_) } | Sort-Object

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('using Microsoft.AspNetCore.Mvc;')
[void]$sb.AppendLine('namespace EasyManufacture.Api.Controllers;')
[void]$sb.AppendLine('/// <summary>')
[void]$sb.AppendLine('/// 旧 APSCore 公共方法的路由壳（由 scripts/Generate-ApsApiStubs.ps1 生成）。')
[void]$sb.AppendLine('/// 实现位于 ApsCoreEngine.LegacyCore / LegacyApi，经 ApsApiLegacyDispatcher 反射调用。')
[void]$sb.AppendLine('/// </summary>')
[void]$sb.AppendLine('public partial class APSAPIController')
[void]$sb.AppendLine('{')

foreach ($name in $missing) {
    $params = $engineMethods[$name]
    if ([string]::IsNullOrWhiteSpace($params)) {
        [void]$sb.AppendLine('    [HttpPost]')
        [void]$sb.AppendLine("    public Task<IActionResult> $name() => InvokeLegacyAsync();")
    }
    else {
        $parts = $params -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        $sigParts = @()
        $passParts = @()
        foreach ($p in $parts) {
            if ($p -match '^(?:string|int|long|bool|decimal|double|float)\s+(\w+)(?:\s*=\s*[^,]+)?$') {
                $pName = $Matches[1]
                $type = ($p -split '\s+')[0]
                $from = if ($type -eq 'string') { '[FromQuery]' } else { '[FromQuery]' }
                $sigParts += "$from $type $pName"
                $passParts += $pName
            }
            else {
                $sigParts = $null
                break
            }
        }
        if ($null -eq $sigParts) { continue }
        [void]$sb.AppendLine('    [HttpPost]')
        $sig = ($sigParts -join ', ')
        $pass = ($passParts -join ', ')
        [void]$sb.AppendLine("    public Task<IActionResult> $name($sig) => InvokeLegacyAsync($pass);")
    }
}

[void]$sb.AppendLine('}')

$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outFile, $sb.ToString(), $utf8)
Write-Host "已生成 $outFile ，新增 $($missing.Count) 个 action"
