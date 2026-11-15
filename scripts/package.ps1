<#
.SYNOPSIS
    Builds dist\Multitasking.zip, which contains Multitasking\ to extract into <game>\mods. This file is the release
    download. Prints the mod version.
#>
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

dotnet build (Join-Path $root 'src\MultitaskingMod\MultitaskingMod.csproj') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed' }
$modOut = Join-Path $root 'src\MultitaskingMod\bin\Release\net10.0'
$modVersion = (Get-Content (Join-Path $modOut 'mod.json') | ConvertFrom-Json).version
$stage = Join-Path $dist 'obj\mod\Multitasking'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item (Join-Path $modOut 'mod.json'), (Join-Path $modOut 'MultitaskingMod.dll') $stage
$zip = Join-Path $dist 'Multitasking.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path $stage -DestinationPath $zip

Get-ChildItem $dist -File | ForEach-Object { '{0,-40} {1,8:N1} MB' -f $_.Name, ($_.Length / 1MB) } | Out-Host
"Multitasking version $modVersion"
