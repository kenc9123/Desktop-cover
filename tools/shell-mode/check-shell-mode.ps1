Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$winlogonPath = 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon'
$shellValue = $null

if (Test-Path $winlogonPath) {
    try {
        $shellValue = Get-ItemPropertyValue -Path $winlogonPath -Name Shell
    }
    catch {
        $shellValue = $null
    }
}

if ([string]::IsNullOrWhiteSpace($shellValue)) {
    $shellValue = 'explorer.exe'
}

Write-Host "Current user shell: $shellValue"

$backupPath = Join-Path $PSScriptRoot 'shell-backup.json'
if (Test-Path $backupPath) {
    Write-Host "Project backup: $backupPath"
    Get-Content -LiteralPath $backupPath
}
else {
    Write-Host "Project backup: not found"
}
