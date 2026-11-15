<#
.SYNOPSIS
    Dev install: builds the mod and deploys it to <game>/mods/Multitasking. The game defaults to the Steam install.
    The mod loader must be installed in that game first (melvor2-modloader: scripts/install.ps1).
    Re-runnable.
#>
param(
    [string]$GameDir,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
if (-not $GameDir) { $GameDir = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 3794500').InstallLocation }
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$GameDir = (Resolve-Path $GameDir).Path

dotnet build (Join-Path $root 'MultitaskingMod.sln') -c $Configuration -p:GameDir="$GameDir" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$modDir = Join-Path $GameDir 'mods\Multitasking'
New-Item -ItemType Directory -Force $modDir | Out-Null
$modOut = Join-Path $root "src\MultitaskingMod\bin\$Configuration\net10.0"
Copy-Item (Join-Path $modOut 'mod.json'), (Join-Path $modOut 'MultitaskingMod.dll'), (Join-Path $modOut 'MultitaskingMod.pdb') $modDir -Force
Write-Host "Deployed Multitasking to $modDir"
