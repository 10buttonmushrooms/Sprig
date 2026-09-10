#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$failures = New-Object System.Collections.Generic.List[string]

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { $script:failures.Add($Message) }
}

function Read-Script {
    param([string]$Name)
    return Get-Content -Raw (Join-Path $PSScriptRoot $Name)
}

# Every PowerShell script must parse in Windows PowerShell 5.1 and PowerShell 7.
Get-ChildItem $PSScriptRoot -Filter '*.ps1' | ForEach-Object {
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors) | Out-Null
    Assert-True ($errors.Count -eq 0) "$($_.Name) has PowerShell parse errors: $($errors -join '; ')"
}

$all = Read-Script 'all.ps1'
Assert-True ($all -notmatch 'Invoke-WebRequest') 'all.ps1 must not download the game APK from an expiring URL.'
Assert-True ($all -match 'input\\Game\.apk') 'all.ps1 must preflight input\Game.apk.'

$fetchDeps = Read-Script 'fetch_deps.ps1'
Assert-True ($fetchDeps -notmatch 'rev-parse\s+HEAD\s+2>\$null') 'fetch_deps.ps1 must not probe an unborn HEAD with a terminating native stderr redirect.'
Assert-True ($fetchDeps -match 'rev-parse.+--verify.+HEAD') 'fetch_deps.ps1 must safely verify whether HEAD exists.'
Assert-True ($fetchDeps -match 'core\.autocrlf') 'fetch_deps.ps1 must pin dependency checkout line endings for patch reproducibility.'

$applyPatches = Read-Script 'apply_patches.ps1'
Assert-True ($applyPatches -match "ErrorActionPreference\s*=\s*'Continue'") 'apply_patches.ps1 must allow git apply --check to return a nonzero exit code without NativeCommandError.'

$install = Read-Script 'install.ps1'
Assert-True ($install -match "ErrorActionPreference\s*=\s*'Continue'") 'install.ps1 must capture adb failure output without NativeCommandError.'

$inject = Read-Script 'inject_mod.ps1'
Assert-True ($inject -match '\.sprig-input-sha256') 'inject_mod.ps1 must invalidate its apktool cache when input/Game.apk changes.'
Assert-True ($inject -match 'registers.+not supported|does not safely support.+registers') 'inject_mod.ps1 must reject unsafe .registers injection instead of corrupting parameter registers.'

# A missing APK should fail locally and must not attempt any network download.
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-pipeline-$([guid]::NewGuid())"
$scripts = Join-Path $sandbox 'work\scripts'
try {
    New-Item -ItemType Directory -Force $scripts | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'all.ps1') $scripts
    'fetch_tools', 'build_mod', 'inject_mod', 'install' | ForEach-Object {
        Set-Content -Path (Join-Path $scripts "$_.ps1") -Value ''
    }

    function Invoke-WebRequest { throw 'NETWORK_CALLED' }

    $message = $null
    try { & (Join-Path $scripts 'all.ps1') } catch { $message = $_.Exception.Message }
    Assert-True ($message -and $message -notmatch 'NETWORK_CALLED' -and $message -match 'Game\.apk') 'Missing Game.apk must produce a local preflight error, not a network request.'

    $inputDir = Join-Path $sandbox 'input'
    New-Item -ItemType Directory -Force $inputDir | Out-Null
    Set-Content -Path (Join-Path $inputDir 'Game.apk') -Value 'test-apk'
    $secondError = $null
    try { & (Join-Path $scripts 'all.ps1') } catch { $secondError = $_.Exception.Message }
    Assert-True (-not $secondError) "all.ps1 should continue when Game.apk exists: $secondError"
} finally {
    Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue
}

# Exercise expected native-command failures under ErrorActionPreference=Stop.
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-native-errors-$([guid]::NewGuid())"
$scripts = Join-Path $sandbox 'work\scripts'
try {
    New-Item -ItemType Directory -Force $scripts | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'fetch_deps.ps1'), (Join-Path $PSScriptRoot 'apply_patches.ps1'), (Join-Path $PSScriptRoot 'install.ps1') $scripts

    function git {
        if ($args -contains 'rev-parse') {
            Write-Error 'fatal: ambiguous argument HEAD'
            $global:LASTEXITCODE = 128
        } else { $global:LASTEXITCODE = 0 }
    }
    $fetchError = $null
    try { & (Join-Path $scripts 'fetch_deps.ps1') } catch { $fetchError = $_.Exception.Message }
    Assert-True (-not $fetchError) "A fresh dependency repository must tolerate its unborn HEAD: $fetchError"

    New-Item -ItemType Directory -Force (Join-Path $sandbox 'deps\Dobby'), (Join-Path $sandbox 'deps\BNM-Android'), (Join-Path $sandbox 'work\patches') | Out-Null
    Set-Content (Join-Path $sandbox 'work\patches\dobby.patch') 'patch'
    Set-Content (Join-Path $sandbox 'work\patches\bnm-android.patch') 'patch'
    function git {
        if ($args -contains '--reverse') {
            Write-Error 'patch does not apply in reverse'
            $global:LASTEXITCODE = 1
        } else { $global:LASTEXITCODE = 0 }
    }
    $patchError = $null
    try { & (Join-Path $scripts 'apply_patches.ps1') } catch { $patchError = $_.Exception.Message }
    Assert-True (-not $patchError) "A pristine patch must tolerate a failed reverse check: $patchError"

    New-Item -ItemType Directory -Force (Join-Path $sandbox 'out') | Out-Null
    Set-Content (Join-Path $sandbox 'out\Sprig.apk') 'apk'
    function adb {
        Write-Error 'Failure [INSTALL_FAILED_UPDATE_INCOMPATIBLE]'
        $global:LASTEXITCODE = 1
    }
    $installError = $null
    try { & (Join-Path $scripts 'install.ps1') } catch { $installError = $_.Exception.Message }
    Assert-True ($installError -eq 'adb install failed (exit 1).') 'adb stderr must reach install.ps1 friendly failure handling.'
} finally {
    Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) pipeline regression test(s) failed."
}

Write-Host 'All Sprig pipeline regression tests passed.'
$global:LASTEXITCODE = 0
