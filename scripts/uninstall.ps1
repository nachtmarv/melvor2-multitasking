<#
.SYNOPSIS
    Dev uninstall: removes the Multitasking mod from the game (default: the Steam install). The mod loader stays
    (melvor2-modloader: scripts/uninstall.ps1 removes it).
#>
param(
    [string]$GameDir
)

$ErrorActionPreference = 'Stop'
if (-not $GameDir) { $GameDir = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 3794500').InstallLocation }
$modDir = Join-Path (Resolve-Path $GameDir).Path 'mods\Multitasking'
if (Test-Path $modDir) {
    Remove-Item $modDir -Recurse -Force
    Write-Host "Removed $modDir"
}
