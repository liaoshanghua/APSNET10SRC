param(
    [string]$TaskName = 'APS'
)

$ErrorActionPreference = 'Stop'

$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($null -eq $task) {
    Write-Host "Task not found: $TaskName"
    exit 0
}

Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
Write-Host "Removed task: $TaskName" -ForegroundColor Green
