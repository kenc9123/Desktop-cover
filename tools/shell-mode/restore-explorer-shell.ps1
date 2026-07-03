param(
    [string]$Shell = '',
    [switch]$KeepBackup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$winlogonPath = 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon'
if (-not (Test-Path $winlogonPath)) {
    New-Item -Path $winlogonPath -Force | Out-Null
}

$backupPath = Join-Path $PSScriptRoot 'shell-backup.json'
if ([string]::IsNullOrWhiteSpace($Shell)) {
    if (Test-Path $backupPath) {
        $backup = Get-Content -LiteralPath $backupPath -Raw | ConvertFrom-Json
        $Shell = [string]$backup.originalShell
    }

    if ([string]::IsNullOrWhiteSpace($Shell)) {
        $Shell = 'explorer.exe'
    }
}

Set-ItemProperty -Path $winlogonPath -Name Shell -Value $Shell

if ((Test-Path $backupPath) -and -not $KeepBackup) {
    Remove-Item -LiteralPath $backupPath -Force
}

Write-Host "Current user shell restored to: $Shell"
Write-Host "This takes effect after sign-out/sign-in or reboot."
Write-Host "You can start Explorer now with: start-explorer-fallback.ps1"
