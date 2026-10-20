#requires -Version 5.1
# -Apk <Name> builds and installs the APK defined by Bloom\<Name>'s [Apk] section.
param([string]$Apk)
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$GameApk = Join-Path $ProjectRoot 'input\Game.apk'

if (-not (Test-Path $GameApk)) {
    throw "Missing $GameApk. Place a legally obtained PvZH 1.65.27 APK at input\Game.apk, then re-run this script."
}

& "$PSScriptRoot\fetch_tools.ps1"
& "$PSScriptRoot\build_mod.ps1" -Apk $Apk
& "$PSScriptRoot\inject_mod.ps1" -Apk $Apk
& "$PSScriptRoot\install.ps1" -Apk $Apk
