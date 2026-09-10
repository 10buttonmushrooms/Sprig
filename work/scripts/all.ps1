#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Apk = Join-Path $ProjectRoot 'input\Game.apk'

if (-not (Test-Path $Apk)) {
    throw "Missing $Apk. Place a legally obtained PvZH 1.65.27 APK at input\Game.apk, then re-run this script."
}

& "$PSScriptRoot\fetch_tools.ps1"
& "$PSScriptRoot\build_mod.ps1"
& "$PSScriptRoot\inject_mod.ps1"
& "$PSScriptRoot\install.ps1"
