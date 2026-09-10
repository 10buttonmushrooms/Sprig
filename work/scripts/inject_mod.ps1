#requires -Version 5.1
<#
.SYNOPSIS
  Injects libsprig.so into the unpacked APK and signs it.

.DESCRIPTION
  Pipeline:
    1. apktool d (cached by input APK hash) -> work\apk-unpacked\
    2. Discover the launcher activity from AndroidManifest.xml.
    3. Copy libsprig.so into the APK.
    4. Patch the launcher activity to call System.loadLibrary("sprig").
       (idempotent — skips if already present).
    5. apktool b -> work\build-out\Game-rebuilt.apk
    6. uber-apk-signer -> work\build-out\signed\
    7. Stage the signed APK at out\Sprig.apk.
#>

$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$ApktoolJar  = Join-Path $ProjectRoot 'tools\apktool\apktool_3.0.2.jar'
$SignerDir   = Join-Path $ProjectRoot 'tools\uber-apk-signer'
$InputApk    = Join-Path $ProjectRoot 'input\Game.apk'
$Unpacked    = Join-Path $ProjectRoot 'work\apk-unpacked'
$BuildOut    = Join-Path $ProjectRoot 'work\build-out'
$LibSo       = Join-Path $BuildOut 'cmake\libsprig.so'
$Rebuilt     = Join-Path $BuildOut 'Game-rebuilt.apk'
$SignedDir   = Join-Path $BuildOut 'signed'
$OutApk      = Join-Path $ProjectRoot 'out\Sprig.apk'

# ---- Preflight ----
foreach ($p in @($ApktoolJar, $InputApk, $LibSo)) {
    if (-not (Test-Path $p)) { throw "Missing required file: $p" }
}

$signerCandidate = Get-ChildItem $SignerDir -Filter 'uber-apk-signer-*.jar' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $signerCandidate) {
    throw "uber-apk-signer not found. Run work\scripts\fetch_tools.ps1 first."
}
$SignerJar = $signerCandidate.FullName

# ---- 1. Unpack (cached by exact APK hash) ----
$Manifest = Join-Path $Unpacked 'AndroidManifest.xml'
$CacheStamp = Join-Path $Unpacked '.sprig-input-sha256'
$inputHash = (Get-FileHash $InputApk -Algorithm SHA256).Hash.ToLowerInvariant()
$cachedHash = $null
if (Test-Path $CacheStamp) {
    $cachedHash = (Get-Content -Raw $CacheStamp).Trim().ToLowerInvariant()
}

$needsUnpack = (-not (Test-Path $Manifest)) -or ($cachedHash -ne $inputHash)
if ($needsUnpack) {
    if (Test-Path $Unpacked) {
        Write-Host 'Input APK changed or cache is incomplete; rebuilding apktool cache...'
        Remove-Item -LiteralPath $Unpacked -Recurse -Force
    } else {
        Write-Host "Unpacking $InputApk (first time)..."
    }

    & java -jar $ApktoolJar d -f -o $Unpacked $InputApk
    if ($LASTEXITCODE -ne 0) { throw "apktool unpack failed (exit $LASTEXITCODE)." }

    [System.IO.File]::WriteAllText($CacheStamp, $inputHash, [System.Text.UTF8Encoding]::new($false))
} else {
    Write-Host "Using cached unpack at $Unpacked"
}

# ---- 2. Discover launcher activity ----
[xml]$x = Get-Content -Raw $Manifest

function Test-IsMainLauncher {
    param($Node)
    foreach ($f in @($Node.'intent-filter')) {
        if (-not $f) { continue }
        $acts = @($f.action | ForEach-Object { $_.name })
        $cats = @($f.category | ForEach-Object { $_.name })
        if ($acts -contains 'android.intent.action.MAIN' -and $cats -contains 'android.intent.category.LAUNCHER') {
            return $true
        }
    }
    return $false
}

$pkg = $x.manifest.package
$app = $x.manifest.application

$launcher = $null
$kind = $null
foreach ($a in @($app.activity)) {
    if ($a -and (Test-IsMainLauncher $a)) { $launcher = $a; $kind = 'activity'; break }
}
if (-not $launcher) {
    foreach ($a in @($app.'activity-alias')) {
        if ($a -and (Test-IsMainLauncher $a)) { $launcher = $a; $kind = 'activity-alias'; break }
    }
}
if (-not $launcher) { throw "Could not find launcher activity in $Manifest" }

$activityName = if ($kind -eq 'activity-alias') { $launcher.targetActivity } else { $launcher.name }
if ($activityName -like '.*') { $activityName = "$pkg$activityName" }
Write-Host "Launcher activity ($kind): $activityName"

# ---- 3. Copy Sprig ----
$LibDir = Join-Path $Unpacked 'lib\arm64-v8a'
if (-not (Test-Path $LibDir)) { New-Item -ItemType Directory -Path $LibDir | Out-Null }
Copy-Item -Force $LibSo (Join-Path $LibDir 'libsprig.so')
Write-Host "Copied libsprig.so -> $LibDir"

# ---- 4. Patch smali (idempotent) ----
$smaliRel = ($activityName -replace '\.', '\') + '.smali'
$smaliFile = $null
foreach ($d in (Get-ChildItem $Unpacked -Directory -Filter 'smali*')) {
    $candidate = Join-Path $d.FullName $smaliRel
    if (Test-Path $candidate) { $smaliFile = $candidate; break }
}
if (-not $smaliFile) { throw "Smali for $activityName not found under $Unpacked\smali*\" }

$rawText = Get-Content -Raw -Path $smaliFile
$alreadyPatched = $rawText -match 'const-string\s+v\d+,\s*"sprig"\s*[\r\n]+\s*invoke-static\s*\{v\d+\}\s*,\s*Ljava/lang/System;->loadLibrary\(Ljava/lang/String;\)V'

if ($alreadyPatched) {
    Write-Host "Smali already contains loadLibrary(`"sprig`") — skipping patch."
} else {
    $lines = Get-Content -Path $smaliFile

    # Find onCreate(Landroid/os/Bundle;)V
    $methodIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\.method\b.*\bonCreate\(Landroid/os/Bundle;\)V\s*$') {
            $methodIdx = $i; break
        }
    }
    if ($methodIdx -lt 0) { throw "Could not find onCreate(Landroid/os/Bundle;)V in $smaliFile" }

    # Find .locals N inside that method. Increasing .registers changes the
    # physical v-register aliases for parameters, so blindly bumping it can
    # corrupt methods that use vN aliases instead of pN. Fail explicitly rather
    # than emit invalid smali. The supported PvZH launcher uses .locals.
    $localsIdx = -1
    $localsCount = 0
    $usesRegisters = $false
    for ($j = $methodIdx + 1; $j -lt $lines.Count; $j++) {
        $t = $lines[$j].TrimStart()
        if ($t -match '^\.end\s+method') { break }
        if ($t -match '^\.locals\s+(\d+)') {
            $localsIdx = $j
            $localsCount = [int]$Matches[1]
            break
        }
        if ($t -match '^\.registers\s+\d+') {
            $localsIdx = $j
            $usesRegisters = $true
            break
        }
    }
    if ($localsIdx -lt 0) { throw "Could not find .locals/.registers in onCreate of $smaliFile" }
    if ($usesRegisters) {
        throw "Automatic smali injection does not safely support .registers in onCreate of $smaliFile; expected .locals."
    }

    # Bump locals by 1 and use the newly allocated free local vN.
    $newCount = $localsCount + 1
    $regIndex = $localsCount

    $indent = ''
    if ($lines[$localsIdx] -match '^(\s+)') { $indent = $Matches[1] } else { $indent = '    ' }
    $lines[$localsIdx] = "$indent.locals $newCount"

    # Walk past method-prologue directives so the injection lands at the first
    # real instruction. Skip annotations as whole blocks.
    $insertIdx = $localsIdx + 1
    while ($insertIdx -lt $lines.Count) {
        $t = $lines[$insertIdx].TrimStart()
        if ($t -eq '' -or $t.StartsWith('#')) { $insertIdx++; continue }
        if ($t -match '^\.(param|local|line|prologue)\b') { $insertIdx++; continue }
        if ($t -match '^\.annotation\b') {
            $insertIdx++
            while ($insertIdx -lt $lines.Count -and ($lines[$insertIdx].TrimStart() -notmatch '^\.end\s+annotation\b')) {
                $insertIdx++
            }
            if ($insertIdx -lt $lines.Count) { $insertIdx++ }
            continue
        }
        break
    }

    $injection = @(
        "$indent# Sprig",
        "${indent}const-string v$regIndex, `"sprig`"",
        "${indent}invoke-static {v$regIndex}, Ljava/lang/System;->loadLibrary(Ljava/lang/String;)V",
        ""
    )

    $newLines = @()
    if ($insertIdx -gt 0) { $newLines += $lines[0..($insertIdx - 1)] }
    $newLines += $injection
    if ($insertIdx -lt $lines.Count) { $newLines += $lines[$insertIdx..($lines.Count - 1)] }

    [System.IO.File]::WriteAllLines($smaliFile, [string[]]$newLines, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Patched onCreate in $smaliFile (.locals $localsCount -> $newCount, used v$regIndex)."
}

# ---- 5. Repack ----
# Keep 1.65.27 installable over the mistakenly installed 1.66.7 build without
# uninstalling or clearing saves. Only Android's package revision changes.
$ApktoolConfig = Join-Path $Unpacked 'apktool.yml'
[System.IO.File]::WriteAllText($ApktoolConfig, ((Get-Content -Raw $ApktoolConfig) -replace '(?m)^  versionCode: 291\r?$', '  versionCode: 293'), [System.Text.UTF8Encoding]::new($false))
if (Test-Path $Rebuilt) { Remove-Item -Force $Rebuilt }
if (-not (Test-Path $BuildOut)) { New-Item -ItemType Directory -Path $BuildOut | Out-Null }
Write-Host "Repacking APK..."
& java -jar $ApktoolJar b -f -o $Rebuilt $Unpacked
if ($LASTEXITCODE -ne 0) { throw "apktool repack failed (exit $LASTEXITCODE)." }

# ---- 6. Sign ----
if (Test-Path $SignedDir) { Remove-Item -Recurse -Force $SignedDir }
New-Item -ItemType Directory -Path $SignedDir | Out-Null
Write-Host "Signing..."
& java -jar $SignerJar --apks $Rebuilt --out $SignedDir
if ($LASTEXITCODE -ne 0) { throw "uber-apk-signer failed (exit $LASTEXITCODE)." }

$signed = Get-ChildItem $SignedDir -Filter '*.apk' | Select-Object -First 1
if (-not $signed) { throw "No signed apk produced in $SignedDir." }

# ---- 7. Stage to out\ ----
$OutDir = Split-Path $OutApk -Parent
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
Copy-Item -Force $signed.FullName $OutApk

$origSize = (Get-Item $InputApk).Length
$outSize  = (Get-Item $OutApk).Length
$delta    = $outSize - $origSize
$sign     = if ($delta -ge 0) { '+' } else { '' }
Write-Host ("Final APK: {0}" -f $OutApk)
Write-Host ("Sizes: input={0:N0}  output={1:N0}  delta={2}{3:N0}" -f $origSize, $outSize, $sign, $delta)
