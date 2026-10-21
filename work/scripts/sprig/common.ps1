#requires -Version 5.1
# Shared by the sprig commands: paths, the compiled C# helpers and the dump index.

$SprigRoot = (Resolve-Path "$PSScriptRoot\..\..\..").Path
$SprigDump = Join-Path $SprigRoot 'work\dump\dump.cs'
$SprigIndexCache = Join-Path $SprigRoot 'work\dump\.sprig-index'
# Tests point the tools at a fixture dump.
if ($env:SPRIG_DUMP) {
    $SprigDump = $env:SPRIG_DUMP
    $SprigIndexCache = "$env:SPRIG_DUMP.sprig-index"
}
$SprigToolsDir = Join-Path $SprigRoot 'work\build-out\sprig'

function Import-SprigTools {
    if ('Sprig.DumpIndex' -as [type]) { return }
    $sources = @('DumpIndex.cs', 'Find.cs', 'CppLint.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ } | Where-Object { Test-Path -LiteralPath $_ }
    $text = ($sources | ForEach-Object { [System.IO.File]::ReadAllText($_) }) -join "`n"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = -join ($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text)) | Select-Object -First 8 | ForEach-Object { $_.ToString('x2') })
    } finally { $sha.Dispose() }
    $edition = if ($PSVersionTable.PSEdition -eq 'Core') { 'core' } else { 'desktop' }
    $dll = Join-Path $SprigToolsDir "SprigTools-$edition-$hash.dll"
    if (Test-Path -LiteralPath $dll) {
        Add-Type -LiteralPath $dll
        return
    }
    try {
        New-Item -ItemType Directory -Force -Path $SprigToolsDir | Out-Null
        Get-ChildItem -LiteralPath $SprigToolsDir -Filter "SprigTools-$edition-*.dll" -ErrorAction SilentlyContinue |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue }
        # Add-Type treats compiler warnings as errors unless told otherwise.
        Add-Type -Path $sources -OutputAssembly $dll -OutputType Library -IgnoreWarnings
        Add-Type -LiteralPath $dll
    } catch {
        # A read-only or locked build directory must not stop the tools.
        if (-not ('Sprig.DumpIndex' -as [type])) { Add-Type -Path $sources -IgnoreWarnings }
    }
}

function Test-SprigDump {
    return (Test-Path -LiteralPath $SprigDump)
}

function Get-SprigIndex {
    if (-not (Test-SprigDump)) {
        throw "work\dump\dump.cs not found. Put the Il2CppDumper dump.cs for input\Game.apk at $SprigDump."
    }
    Import-SprigTools
    return [Sprig.DumpIndex]::LoadOrBuild($SprigDump, $SprigIndexCache)
}
