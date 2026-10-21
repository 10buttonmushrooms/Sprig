#requires -Version 5.1
<#
.SYNOPSIS
  Installs out\Sprig.apk on the connected device via adb.
#>

$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$OutApk      = Join-Path $ProjectRoot 'out\Sprig.apk'
$Package     = 'com.ea.gp.pvzheroes'

if (-not (Test-Path $OutApk)) {
    throw "$OutApk not found. Run work\scripts\inject_mod.ps1 (or all.ps1) first."
}

Write-Host "Installing $OutApk ..."
$oldPreference = $ErrorActionPreference
try {
    # adb commonly writes useful failure diagnostics to stderr. Windows
    # PowerShell 5.1 must be allowed to capture those diagnostics so the code
    # below can inspect the exit status and explain signature mismatches.
    $ErrorActionPreference = 'Continue'
    $installOutput = @(& adb install -r $OutApk 2>&1)
    $installCode   = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $oldPreference
}
$installOutput | ForEach-Object { Write-Host $_ }

if ($installCode -ne 0 -or ($installOutput -join "`n") -match 'INSTALL_FAILED') {
    if (($installOutput -join "`n") -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE|signatures do not match') {
        Write-Warning "Signature mismatch with the installed app."
        Write-Warning "Run: adb uninstall $Package"
        Write-Warning "Then re-run this script. (Uninstalling will wipe game save data.)"
    }
    throw "adb install failed (exit $installCode)."
}

Write-Host "Install OK."
Write-Host "Launch the game on the device and watch: adb logcat -s Sprig"
