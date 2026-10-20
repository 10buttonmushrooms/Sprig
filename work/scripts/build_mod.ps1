#requires -Version 5.1
<#
.SYNOPSIS
  Builds libsprig.so for arm64-v8a using the Android NDK.

.DESCRIPTION
  Reads $env:ANDROID_NDK_ROOT and builds with CMake and Ninja. -Apk <Name> adds the
  Bloom\<Name> APK module, after running its setup.ps1 if it has one.
#>
param([string]$Apk)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\native.ps1"
. "$PSScriptRoot\apk.ps1"

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Target      = Get-SprigApk $Apk
$ModDir      = Join-Path $ProjectRoot 'mod'
$BuildOut    = $Target.BuildOut
$CmakeDir    = $Target.CMakeDir
$BloomApk    = if ($Target.Module) { $Target.Name } else { '' }

if (-not $env:ANDROID_NDK_ROOT) {
    throw 'ANDROID_NDK_ROOT is not set. Point it at your Android NDK (e.g. C:\Android\ndk\android-ndk-r27d).'
}

$Toolchain = Join-Path $env:ANDROID_NDK_ROOT 'build\cmake\android.toolchain.cmake'
if (-not (Test-Path $Toolchain)) {
    throw "Toolchain file not found: $Toolchain"
}

if (-not (Test-Path $BuildOut)) {
    New-Item -ItemType Directory -Path $BuildOut | Out-Null
}

# An APK module may need large assets that are fetched rather than committed.
if ($Target.Module) {
    $Setup = Join-Path $Target.Module 'setup.ps1'
    if (Test-Path -LiteralPath $Setup) { & $Setup }
}

# Fetch the pinned dependencies and apply their compatibility patches.
& "$PSScriptRoot\fetch_deps.ps1"

& "$PSScriptRoot\apply_patches.ps1"

# Configure (idempotent — CMake caches).
Invoke-Native cmake `
    -S $ModDir `
    -B $CmakeDir `
    -G Ninja `
    "-DCMAKE_TOOLCHAIN_FILE=$Toolchain" `
    '-DANDROID_ABI=arm64-v8a' `
    '-DANDROID_PLATFORM=android-21' `
    '-DCMAKE_BUILD_TYPE=Release' `
    "-DBLOOM_APK=$BloomApk"

# Build.
Invoke-Native cmake --build $CmakeDir --config Release

$BuiltSo = Join-Path $CmakeDir 'libsprig.so'
if (-not (Test-Path $BuiltSo)) { throw "Expected output not found: $BuiltSo" }
Write-Host "Built $BuiltSo"
