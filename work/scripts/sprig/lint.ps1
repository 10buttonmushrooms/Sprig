#requires -Version 5.1
# sprig lint [Module...] [--dir <path>]... [--no-compile] [--compile-all]
# Checks Bloom modules (and mod\src) against work\dump\dump.cs. Exit codes: 0 clean or warnings
# only, 1 errors, 2 tool failure. build_mod.ps1 runs this before compiling.
#   --dir <path>     lint a module folder outside Bloom\ (tests, examples)
#   --no-compile     text checks only
#   --compile-all    also compile sources that have no generated checks (catches compile errors)
# Capture arguments first: dot-sourcing a script replaces $args.
$argv = [string[]]@($args)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"
. "$SprigRoot\work\scripts\native.ps1"

$noCompile = $false
$compileAll = $false
$named = New-Object System.Collections.Generic.List[string]
$dirs = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $argv.Count; $i++) {
    $a = $argv[$i]
    if ($a -eq '--no-compile') { $noCompile = $true }
    elseif ($a -eq '--compile-all') { $compileAll = $true }
    elseif ($a -eq '--dir' -and $i + 1 -lt $argv.Count) { $dirs.Add([System.IO.Path]::GetFullPath($argv[++$i])) }
    elseif ($a -like '--*') { Write-Output "unknown option $a"; exit 2 }
    # Module names follow Bloom's rule, so a name never points outside Bloom\.
    elseif ($a -cnotmatch '^[A-Za-z][A-Za-z0-9_-]*$') { Write-Output "invalid module name '$a': use the folder name under Bloom\, such as TutorialSkip"; exit 2 }
    else { $named.Add($a) }
}

function Get-CompileEntries {
    # Configures CMake once into work\build-out\lint and returns compile_commands.json entries.
    param($Notes)
    if (-not $env:ANDROID_NDK_ROOT) {
        $Notes.Add('note: ANDROID_NDK_ROOT is not set, so the compiler checks (hook-signature, custom-class layout) were skipped')
        return $null
    }
    if (-not (Test-Path (Join-Path $SprigRoot 'deps\BNM-Android\include'))) {
        $Notes.Add('note: deps\ is missing (run work\scripts\build_mod.ps1 once), so the compiler checks were skipped')
        return $null
    }
    $lintDir = Join-Path $SprigRoot 'work\build-out\lint'
    $ccj = Join-Path $lintDir 'compile_commands.json'
    New-Item -ItemType Directory -Force -Path $lintDir | Out-Null
    $log = Join-Path $lintDir 'configure.log'
    try {
        if (Test-Path (Join-Path $lintDir 'build.ninja')) {
            # Re-runs CMake only when its inputs (module lists, sources) changed.
            Invoke-Native ninja -C $lintDir build.ninja *> $log
        } else {
            $toolchain = Join-Path $env:ANDROID_NDK_ROOT 'build\cmake\android.toolchain.cmake'
            Invoke-Native cmake -S (Join-Path $SprigRoot 'mod') -B $lintDir -G Ninja "-DCMAKE_TOOLCHAIN_FILE=$toolchain" `
                '-DANDROID_ABI=arm64-v8a' '-DANDROID_PLATFORM=android-21' '-DCMAKE_BUILD_TYPE=Release' '-DCMAKE_EXPORT_COMPILE_COMMANDS=ON' *> $log
        }
    } catch {
        $Notes.Add("note: configuring the compiler checks failed (see $log), so they were skipped")
        return $null
    }
    if (-not (Test-Path $ccj)) { $Notes.Add('note: CMake wrote no compile_commands.json, so the compiler checks were skipped'); return $null }
    return @(Get-Content -Raw -LiteralPath $ccj | ConvertFrom-Json)
}

function Get-EntryFor {
    # The compile command for a source; files outside the build borrow flags from a module entry
    # (or mod\src\main.cpp) and get their own module name, type and include folders.
    param($Entries, $File)
    $entry = $Entries | Where-Object { [System.IO.Path]::GetFullPath($_.file) -ieq $File.Path } | Select-Object -First 1
    if ($entry) { return @{ Directory = [string]$entry.directory; Command = [Sprig.CommandLine]::Split([string]$entry.command) } }
    $donor = $null
    if ($File.ModuleDir) {
        $donor = $Entries | Where-Object { [System.IO.Path]::GetFullPath($_.file).StartsWith($File.ModuleDir, [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    }
    if (-not $donor) { $donor = $Entries | Where-Object { $_.file -match 'mod[\\/]src[\\/]main\.cpp$' } | Select-Object -First 1 }
    if (-not $donor) { return $null }
    $command = [Sprig.CommandLine]::Split([string]$donor.command)
    $kept = New-Object System.Collections.Generic.List[string]
    foreach ($arg in $command) { if ($arg -notlike '-DBLOOM_MODULE_NAME=*' -and $arg -notlike '-DBLOOM_MODULE_TYPE=*') { $kept.Add($arg) } }
    if ($File.ModuleName) {
        $last = $kept.Count - 1
        $extra = @("-DBLOOM_MODULE_NAME=`"$($File.ModuleName)`"")
        if ($File.ModuleType) { $extra += "-DBLOOM_MODULE_TYPE=::Bloom::Type::$($File.ModuleType)" }
        foreach ($inc in 'inc', 'include') {
            $d = Join-Path $File.ModuleDir $inc
            if (Test-Path -LiteralPath $d) { $extra += "-I$d" }
        }
        $kept.InsertRange($last, [string[]]$extra)
    }
    return @{ Directory = [string]$donor.directory; Command = $kept }
}

$notes = New-Object System.Collections.Generic.List[string]
try {
    Import-SprigTools
    $index = $null
    if (Test-SprigDump) { $index = Get-SprigIndex }
    else { $notes.Add('note: work\dump\dump.cs is missing, so the names, hook-signature, overload, custom-class, enum-range and pool-bypass rules were skipped') }

    $linter = New-Object Sprig.Linter -ArgumentList $index, $SprigRoot
    $bloom = Join-Path $SprigRoot 'Bloom'
    foreach ($d in $dirs) {
        if (-not (Test-Path -LiteralPath $d -PathType Container)) { Write-Output "no folder $d"; exit 2 }
        $linter.LintModule($d)
    }
    if ($named.Count -gt 0) {
        foreach ($name in $named) {
            $dir = Join-Path $bloom $name
            if (-not (Test-Path -LiteralPath $dir -PathType Container)) { Write-Output "no module Bloom\$name"; exit 2 }
            $linter.LintModule($dir)
        }
    } elseif ($dirs.Count -eq 0) {
        Get-ChildItem -LiteralPath $bloom -Directory | Where-Object { $_.Name -notlike '_*' } | Sort-Object Name |
            ForEach-Object { $linter.LintModule($_.FullName) }
        $core = @(Get-ChildItem -LiteralPath (Join-Path $SprigRoot 'mod\src') -Filter '*.cpp' -File | ForEach-Object FullName)
        if ($core.Count -gt 0) { $linter.LintFiles([string[]]$core) }
    }

    # ---- compiler tier ----
    $toCompile = @($linter.FilesWithChecks($compileAll))
    if ($toCompile.Count -gt 0 -and -not $noCompile) {
        $entries = Get-CompileEntries -Notes $notes
        if ($entries) {
            $checkDir = Join-Path $SprigRoot 'work\build-out\lint\checks'
            if (Test-Path -LiteralPath $checkDir) { Remove-Item -LiteralPath $checkDir -Recurse -Force }
            New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
            foreach ($file in $toCompile) {
                $entry = Get-EntryFor -Entries $entries -File $file
                if (-not $entry) { $notes.Add("note: no compile flags for $($file.RelPath), so its compiler checks were skipped"); continue }
                # A failure here is reported as a lint-internal warning for this file; the others still run.
                try {
                    $checks = $null; $first = 0
                    $unitText = $linter.CheckUnit($file, $compileAll, [ref]$checks, [ref]$first)
                    if (-not $unitText) { continue }
                    $unit = Join-Path $checkDir (($file.RelPath -replace '[\\/:]', '_') + '.lint.cpp')
                    [System.IO.File]::WriteAllText($unit, $unitText, [System.Text.UTF8Encoding]::new($false))
                    $command = $entry.Command
                    $clang = $command[0]
                    $rsp = "$unit.rsp"
                    [System.IO.File]::WriteAllText($rsp, [Sprig.CommandLine]::ResponseFile([Sprig.CommandLine]::SyntaxOnly($command, $unit)), [System.Text.UTF8Encoding]::new($false))
                    $old = $ErrorActionPreference
                    try {
                        $ErrorActionPreference = 'Continue'
                        Push-Location -LiteralPath $entry.Directory
                        $output = (& $clang '--rsp-quoting=posix' "@$rsp" 2>&1 | ForEach-Object { "$_" }) -join "`n"
                    } finally {
                        Pop-Location
                        $ErrorActionPreference = $old
                    }
                    $global:LASTEXITCODE = 0
                    $linter.ParseCompilerOutput($file, $unit, $output)
                } catch {
                    $linter.InternalFailure($file.RelPath, 'the compiler checks failed to run on this file', $_.Exception)
                }
            }
        }
    }
} catch {
    Write-Output "lint failed: $($_.Exception.Message)"
    Write-Output 'Build without lint with: .\work\scripts\build_mod.ps1 -NoLint'
    exit 2
}

$notes | ForEach-Object { Write-Output $_ }
Write-Output $linter.Report()
exit $(if ($linter.Errors -gt 0) { 1 } else { 0 })
