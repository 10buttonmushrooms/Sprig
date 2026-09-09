#requires -Version 5.1
<#
.SYNOPSIS
  Builds libsprig.so for arm64-v8a using the Android NDK.

.DESCRIPTION
  Reads $env:ANDROID_NDK_ROOT and builds with CMake and Ninja.
#>

$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$ModDir      = Join-Path $ProjectRoot 'mod'
$BuildOut    = Join-Path $ProjectRoot 'work\build-out'
$CmakeDir    = Join-Path $BuildOut 'cmake'

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

# Fetch the pinned dependencies and apply their compatibility patches.
& "$PSScriptRoot\fetch_deps.ps1"
if ($LASTEXITCODE -ne 0) { throw "fetch_deps.ps1 failed (exit $LASTEXITCODE)." }

& "$PSScriptRoot\apply_patches.ps1"
if ($LASTEXITCODE -ne 0) { throw "apply_patches.ps1 failed (exit $LASTEXITCODE)." }

# Configure (idempotent — CMake caches).
& cmake `
    -S $ModDir `
    -B $CmakeDir `
    -G Ninja `
    "-DCMAKE_TOOLCHAIN_FILE=$Toolchain" `
    '-DANDROID_ABI=arm64-v8a' `
    '-DANDROID_PLATFORM=android-21' `
    '-DCMAKE_BUILD_TYPE=Release'
if ($LASTEXITCODE -ne 0) { throw "CMake configure failed (exit $LASTEXITCODE)." }

# Build.
& cmake --build $CmakeDir --config Release
if ($LASTEXITCODE -ne 0) { throw "CMake build failed (exit $LASTEXITCODE)." }

$BuiltSo = Join-Path $CmakeDir 'libsprig.so'
if (-not (Test-Path $BuiltSo)) { throw "Expected output not found: $BuiltSo" }
Write-Host "Built $BuiltSo"
