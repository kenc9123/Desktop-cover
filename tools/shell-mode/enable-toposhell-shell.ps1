param(
    [string]$TopoShellExe = '',
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$defaultExe = Join-Path $projectRoot 'src\TopoShell.App\bin\Debug\net8.0-windows10.0.19041.0\TopoShell.App.exe'
if ([string]::IsNullOrWhiteSpace($TopoShellExe)) {
    $TopoShellExe = $defaultExe
}

$resolvedExe = Resolve-Path -LiteralPath $TopoShellExe -ErrorAction SilentlyContinue
if ($null -eq $resolvedExe) {
    throw "TopoShell executable was not found: $TopoShellExe. Build the app first."
}

$topoShellPath = $resolvedExe.Path
$winlogonPath = 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon'
if (-not (Test-Path $winlogonPath)) {
    New-Item -Path $winlogonPath -Force | Out-Null
}

try {
    $currentShell = Get-ItemPropertyValue -Path $winlogonPath -Name Shell
}
catch {
    $currentShell = $null
}
if ([string]::IsNullOrWhiteSpace($currentShell)) {
    $currentShell = 'explorer.exe'
}

$backupPath = Join-Path $PSScriptRoot 'shell-backup.json'
if ((Test-Path $backupPath) -and -not $Force) {
    throw "Backup already exists at $backupPath. Use -Force only if you intentionally want to overwrite it."
}

$backup = [ordered]@{
    timestamp = (Get-Date).ToString('o')
    registryPath = 'HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon'
    valueName = 'Shell'
    originalShell = $currentShell
    topoShell = $topoShellPath
}

$backup | ConvertTo-Json | Set-Content -LiteralPath $backupPath -Encoding UTF8
Set-ItemProperty -Path $winlogonPath -Name Shell -Value $topoShellPath

Write-Host "TopoShell is now configured as the current user's shell."
Write-Host "This takes effect after sign-out/sign-in or reboot."
Write-Host "Backup written to: $backupPath"
Write-Host "Recovery script: $(Join-Path $PSScriptRoot 'restore-explorer-shell.ps1')"
