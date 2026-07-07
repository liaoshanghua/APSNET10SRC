#Requires -Version 5.1
<#
.SYNOPSIS
  仅 UPDATE Dev_DictionaryField：Comment、FieldIndex、Width、IsVisible（图片无的栏位 IsVisible=0）
.EXAMPLE
  .\Update-DictionaryFieldComments.ps1 `
    -DictionaryId 12345 `
    -MappingPath "..\docs\sql\V_APS_MOPlanGroupProcessTimeline-field-mapping.json"
#>
param(
    [Parameter(Mandatory = $true)]
    [int] $DictionaryId,

    [Parameter(Mandatory = $true)]
    [string] $MappingPath,

    [string] $DictionaryExportPath = (Join-Path $PSScriptRoot '..\docs\字典V4-export.csv'),

    [int] $DefaultWidth = 80,

    [string] $ConnectionString = $env:APS_CONNECTION_STRING
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $MappingPath)) {
    throw "Mapping file not found: $MappingPath"
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Set APS_CONNECTION_STRING or pass -ConnectionString"
}

function Parse-CsvLine([string]$Line) {
    [regex]::Matches($Line, '(?:^|,)("(?:[^"]|"")*"|[^,]*)') | ForEach-Object {
        $_.Value.TrimStart(',').Trim('"').Replace('""', '"')
    }
}

function Get-LatestWidthMap {
    param([string]$CsvPath)

    if (-not (Test-Path -LiteralPath $CsvPath)) {
        Write-Warning "Dictionary export not found: $CsvPath — Width will use JSON or default only."
        return @{}
    }

    $lines = Get-Content -LiteralPath $CsvPath -Encoding UTF8
    $header = $lines[0]
    $idx = @{}
    $i = 0
    foreach ($h in ($header -split ',')) { $idx[$h] = $i; $i++ }

    $map = @{}
    foreach ($line in $lines[1..($lines.Count - 1)]) {
        $c = Parse-CsvLine $line
        $pn = $c[$idx['参数名称']]
        if ([string]::IsNullOrWhiteSpace($pn)) { continue }

        $widthText = $c[$idx['宽度']]
        $modifyText = $c[$idx['修改日期']]
        $rowId = $c[$idx['ID']]

        if ([string]::IsNullOrWhiteSpace($widthText)) { continue }
        if (-not [int]::TryParse($widthText, [ref]$null)) { continue }

        $width = [int]$widthText
        $modifyKey = if ([string]::IsNullOrWhiteSpace($modifyText)) { '' } else { $modifyText }
        $idKey = if ([string]::IsNullOrWhiteSpace($rowId)) { 0 } else { [int]$rowId }

        if (-not $map.ContainsKey($pn)) {
            $map[$pn] = @{ Width = $width; Modify = $modifyKey; Id = $idKey }
            continue
        }

        $cur = $map[$pn]
        $replace = $false
        if ($modifyKey -gt $cur.Modify) { $replace = $true }
        elseif ($modifyKey -eq $cur.Modify -and $idKey -gt $cur.Id) { $replace = $true }

        if ($replace) {
            $map[$pn] = @{ Width = $width; Modify = $modifyKey; Id = $idKey }
        }
    }
    return $map
}

function Resolve-Width {
    param(
        [string]$ParameterName,
        [hashtable]$WidthMap,
        [object]$MappingItem,
        [int]$Default
    )

    if ($WidthMap.ContainsKey($ParameterName)) {
        return [int]$WidthMap[$ParameterName].Width
    }

    # 日期类透视列：参考 StartDate / EndDate
    if ($ParameterName -like '*StartDate') {
        if ($WidthMap.ContainsKey('StartDate')) { return [int]$WidthMap['StartDate'].Width }
    }
    if ($ParameterName -like '*EndDate') {
        if ($WidthMap.ContainsKey('EndDate')) { return [int]$WidthMap['EndDate'].Width }
    }
    if ($ParameterName -eq 'GroupDisplay') {
        if ($WidthMap.ContainsKey('RemarkSalesOrderInfo')) { return [int]$WidthMap['RemarkSalesOrderInfo'].Width }
        if ($WidthMap.ContainsKey('OrderNo')) { return [int]$WidthMap['OrderNo'].Width }
    }

    if ($null -ne $MappingItem.width -and [int]$MappingItem.width -gt 0) {
        return [int]$MappingItem.width
    }

    return $Default
}

$items = Get-Content -LiteralPath $MappingPath -Encoding UTF8 | ConvertFrom-Json
if (-not $items) { throw "Empty mapping file" }

$widthMap = Get-LatestWidthMap -CsvPath $DictionaryExportPath

$conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
$conn.Open()
try {
    $updated = 0
    foreach ($item in $items) {
        $width = Resolve-Width -ParameterName $item.parameterName -WidthMap $widthMap -MappingItem $item -Default $DefaultWidth

        $cmd = $conn.CreateCommand()
        $cmd.CommandText = @"
UPDATE dbo.Dev_DictionaryField
SET    Comment = @Comment,
       FieldIndex = @FieldIndex,
       Width = @Width,
       IsVisible = 1
WHERE  DictionaryID = @DictionaryId
       AND ParameterName = @ParameterName;
"@
        [void]$cmd.Parameters.AddWithValue('@Comment', [string]$item.comment)
        [void]$cmd.Parameters.AddWithValue('@FieldIndex', [int]$item.fieldIndex)
        [void]$cmd.Parameters.AddWithValue('@Width', $width)
        [void]$cmd.Parameters.AddWithValue('@DictionaryId', $DictionaryId)
        [void]$cmd.Parameters.AddWithValue('@ParameterName', [string]$item.parameterName)
        $rows = $cmd.ExecuteNonQuery()
        $updated += $rows
        Write-Host ("{0,-22} Comment={1} FieldIndex={2} Width={3} (rows={4})" -f $item.parameterName, $item.comment, $item.fieldIndex, $width, $rows)
    }
    Write-Host "DictionaryId=$DictionaryId total updated rows: $updated"

    $hideCmd = $conn.CreateCommand()
    $inList = ($items | ForEach-Object { "'$($_.parameterName -replace '''','''''')'" }) -join ','
    $hideCmd.CommandText = @"
UPDATE dbo.Dev_DictionaryField
SET    IsVisible = 0
WHERE  DictionaryID = @DictionaryId
       AND ParameterName NOT IN ($inList);
"@
    [void]$hideCmd.Parameters.AddWithValue('@DictionaryId', $DictionaryId)
    $hidden = $hideCmd.ExecuteNonQuery()
    Write-Host "Hidden (IsVisible=0) rows: $hidden"

    $list = $conn.CreateCommand()
    $list.CommandText = @"
SELECT ParameterName, Comment, FieldIndex, Width, IsVisible
FROM   dbo.Dev_DictionaryField
WHERE  DictionaryID = @DictionaryId
ORDER  BY FieldIndex, ParameterName
"@
    [void]$list.Parameters.AddWithValue('@DictionaryId', $DictionaryId)
    $reader = $list.ExecuteReader()
    Write-Host '--- verify (visible) ---'
    while ($reader.Read()) {
        if ($reader['IsVisible'] -eq $true -or $reader['IsVisible'] -eq 1) {
            Write-Host ("{0,4} | {1,-22} | {2,-10} | W={3}" -f $reader['FieldIndex'], $reader['ParameterName'], $reader['Comment'], $reader['Width'])
        }
    }
    $reader.Close()
}
finally {
    $conn.Close()
}
