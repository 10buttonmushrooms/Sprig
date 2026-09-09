#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Apk = Join-Path $ProjectRoot 'input\Game.apk'
if (-not (Test-Path $Apk)) { throw "Bundled APK is missing: $Apk" }

& "$PSScriptRoot\fetch_tools.ps1"
& "$PSScriptRoot\build_mod.ps1"
& "$PSScriptRoot\inject_mod.ps1"
& "$PSScriptRoot\install.ps1"
