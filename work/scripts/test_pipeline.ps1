#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$failures = New-Object System.Collections.Generic.List[string]

# A successful native command can write stderr. Capturing a parent script's
# output must not turn that diagnostic into a PS 5.1 terminating error.
. "$PSScriptRoot\native.ps1"
$nativeOutput = @(Invoke-Native cmd.exe /c 'echo native diagnostic 1>&2 & exit /b 0' 2>&1)
if (($nativeOutput -join "`n") -notmatch 'native diagnostic') { throw 'Native stderr was lost.' }
$nativeFailure = $null
try { Invoke-Native cmd.exe /c 'echo native failure 1>&2 & exit /b 9' 2>&1 | Out-Null }
catch { $nativeFailure = $_.Exception.Message }
if ($nativeFailure -ne 'cmd.exe failed (exit 9).') { throw 'Native failures must use the exit code.' }
$nativeFlags = @(Invoke-Native cmd.exe /c echo -f -o)
if (($nativeFlags -join "`n").Trim() -ne '-f -o') { throw 'Native flags must pass through unchanged.' }

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
    Copy-Item (Join-Path $PSScriptRoot 'fetch_deps.ps1'), (Join-Path $PSScriptRoot 'apply_patches.ps1'), (Join-Path $PSScriptRoot 'install.ps1'), (Join-Path $PSScriptRoot 'native.ps1'), (Join-Path $PSScriptRoot 'apk.ps1') $scripts

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

# APK modules resolve their own paths and identity; edits must not stack across runs.
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-apk-$([guid]::NewGuid())"
try {
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'work\scripts'), (Join-Path $sandbox 'Bloom\Demo'), (Join-Path $sandbox 'Bloom\Plain') | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'apk.ps1') (Join-Path $sandbox 'work\scripts')
    . (Join-Path $sandbox 'work\scripts\apk.ps1')
    Add-Type -AssemblyName System.Drawing
    function New-TestPng {
        param([string]$Path, [int]$Size, [System.Drawing.Color]$Color)
        New-Item -ItemType Directory -Force (Split-Path $Path) | Out-Null
        $bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.Clear($Color)
        $graphics.Dispose()
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
    }
    function Get-TestPixel {
        param([string]$Path, [int]$X, [int]$Y)
        $bitmap = New-Object System.Drawing.Bitmap($Path)
        try { return $bitmap.GetPixel($X, $Y) } finally { $bitmap.Dispose() }
    }

    [System.IO.File]::WriteAllText((Join-Path $sandbox 'Bloom\Demo\bloom.ini'), "[Bloom]`nType=PnP`n[Apk]`nLabel = Demo & Co ; launcher name`nPackage=com.ea.gp.pvzheroes.demo`nIcon=icon.png`n")
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'Bloom\Plain\bloom.ini'), "[Bloom]`nType=PnP`n[apk]`nLabel=Wrong case`n")
    New-TestPng (Join-Path $sandbox 'Bloom\Demo\icon.png') 64 ([System.Drawing.Color]::FromArgb(255, 0, 0))

    $default = Get-SprigApk ''
    Assert-True ($default.Name -eq 'Sprig' -and $default.OutApk -like '*\out\Sprig.apk' -and $default.CMakeDir -like '*\work\build-out\cmake' -and -not $default.Module) 'The default APK must keep the original Sprig paths.'
    $demo = Get-SprigApk 'Demo'
    Assert-True ($demo.Label -eq 'Demo & Co' -and $demo.Package -eq 'com.ea.gp.pvzheroes.demo') "APK modules must read [Apk] Label and Package: $($demo.Label) / $($demo.Package)"
    Assert-True ($demo.OutApk -like '*\out\Demo.apk' -and $demo.Unpacked -like '*\work\apk-unpacked-Demo' -and $demo.CMakeDir -like '*\work\build-out\apk\Demo\cmake') 'APK modules must build in their own directories.'
    foreach ($bad in @('Plain', 'Missing', 'Sprig', '..\Demo')) {
        $resolveError = $null
        try { Get-SprigApk $bad | Out-Null } catch { $resolveError = $_.Exception.Message }
        Assert-True ([bool]$resolveError) "Get-SprigApk must reject $bad."
    }

    $unpacked = $demo.Unpacked
    New-Item -ItemType Directory -Force (Join-Path $unpacked 'res\values') | Out-Null
    $pristine = Join-Path $unpacked '.sprig-AndroidManifest.xml'
    [System.IO.File]::WriteAllText($pristine, @'
<manifest package="com.ea.gp.pvzheroes"><permission android:name="com.ea.gp.pvzheroes.permission.C2D_MESSAGE"/><application>
        <meta-data android:name="com.ea.nimble.configuration" android:value="@string/nimble_configuration"/>
        <provider android:authorities="com.ea.gp.pvzheroes.androidx-startup"/><data android:scheme="com.ea.gp.pvzheroes"/>
</application></manifest>
'@)
    [System.IO.File]::WriteAllText((Join-Path $unpacked 'res\values\strings.xml'), '<resources><string name="app_name">PvZ Heroes</string></resources>')
    foreach ($icon in @(@('mipmap-mdpi\ic_launcher.png', 48), @('mipmap-mdpi\ic_launcher_round.png', 48), @('mipmap-mdpi\ic_launcher_foreground.png', 108), @('mipmap-mdpi\ic_launcher_background.png', 108), @('drawable\app_icon.png', 96))) {
        New-TestPng (Join-Path $unpacked ('res\' + $icon[0])) $icon[1] ([System.Drawing.Color]::Blue)
    }
    Set-ApkIdentity -Target $demo -PristineManifest $pristine | Out-Null
    Set-ApkIdentity -Target $demo -PristineManifest $pristine | Out-Null
    $manifest = Get-Content -Raw (Join-Path $unpacked 'AndroidManifest.xml')
    Assert-True ($manifest -match 'package="com\.ea\.gp\.pvzheroes\.demo"' -and $manifest -match '"com\.ea\.gp\.pvzheroes\.demo\.permission\.C2D_MESSAGE"' -and $manifest -match '"com\.ea\.gp\.pvzheroes\.demo\.androidx-startup"' -and $manifest -match 'scheme="com\.ea\.gp\.pvzheroes\.demo"') 'Every package reference must move to the APK package.'
    Assert-True ($manifest -notmatch 'demo\.demo' -and ([regex]::Matches($manifest, 'NimbleBundleIdOverride')).Count -eq 1 -and $manifest -match 'NimbleBundleIdOverride" android:value="com\.ea\.gp\.pvzheroes"') 'APK identity edits must not stack and must keep Nimble on the original bundle id.'
    $strings = Get-Content -Raw (Join-Path $unpacked 'res\values\strings.xml')
    Assert-True ($strings -match '<string name="app_name">Demo &amp; Co</string>') "The launcher label must be XML-escaped: $strings"
    $red = { param($p) $p.R -gt 200 -and $p.G -lt 40 -and $p.B -lt 40 }
    Assert-True (& $red (Get-TestPixel (Join-Path $unpacked 'res\mipmap-mdpi\ic_launcher.png') 1 1)) 'Square launcher icons must show the APK icon edge to edge.'
    Assert-True ((Get-TestPixel (Join-Path $unpacked 'res\mipmap-mdpi\ic_launcher_round.png') 0 0).A -eq 0) 'Round launcher icons must be transparent outside the circle.'
    $foreground = Join-Path $unpacked 'res\mipmap-mdpi\ic_launcher_foreground.png'
    Assert-True ((Get-TestPixel $foreground 5 5).A -eq 0 -and (& $red (Get-TestPixel $foreground 54 54))) 'Adaptive foregrounds must keep the icon inside the 72dp safe zone.'
    Assert-True (& $red (Get-TestPixel (Join-Path $unpacked 'res\mipmap-mdpi\ic_launcher_background.png') 0 0)) 'Adaptive backgrounds must use the icon edge color.'
    [string[]]$sizes = @(Get-ChildItem (Join-Path $unpacked 'res') -Recurse -Filter '*.png' | ForEach-Object { $i = [System.Drawing.Image]::FromFile($_.FullName); "$($_.Name)=$($i.Width)"; $i.Dispose() })
    [Array]::Sort($sizes, [System.StringComparer]::Ordinal)
    Assert-True (($sizes -join ',') -ceq 'app_icon.png=96,ic_launcher.png=48,ic_launcher_background.png=108,ic_launcher_foreground.png=108,ic_launcher_round.png=48') "Icons must keep their density sizes: $($sizes -join ',')"
} finally {
    Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue
}

$buildMod = Read-Script 'build_mod.ps1'
Assert-True ($buildMod -match 'BLOOM_APK=' -and $buildMod -match 'setup\.ps1') 'build_mod.ps1 must select the APK module and run its setup.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) pipeline regression test(s) failed."
}

Write-Host 'All Sprig pipeline regression tests passed.'
$global:LASTEXITCODE = 0
