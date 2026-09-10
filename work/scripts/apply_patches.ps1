#requires -Version 5.1
<#
.SYNOPSIS
  Idempotently applies the Dobby and BNM-Android compatibility patches.

.DESCRIPTION
  Safe to run repeatedly against dependencies fetched by fetch_deps.ps1.
#>

$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$PatchDir    = Join-Path $ProjectRoot 'work\patches'

$targets = @(
    @{ Name = 'Dobby';        Repo = Join-Path $ProjectRoot 'deps\Dobby';        Patch = Join-Path $PatchDir 'dobby.patch' }
    @{ Name = 'BNM-Android';  Repo = Join-Path $ProjectRoot 'deps\BNM-Android';  Patch = Join-Path $PatchDir 'bnm-android.patch' }
)

function Test-GitApply {
    param([string]$Repo, [string]$Patch, [switch]$Reverse)

    $args = @('apply', '--check')
    if ($Reverse) { $args += '--reverse' }
    $args += $Patch

    $oldPreference = $ErrorActionPreference
    try {
        # A failed --check is expected while deciding whether a patch is
        # pristine or already applied. Windows PowerShell 5.1 otherwise turns
        # redirected native stderr into a terminating NativeCommandError.
        $ErrorActionPreference = 'Continue'
        & git -C $Repo @args 2>$null
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldPreference
    }

    return ($code -eq 0)
}

foreach ($t in $targets) {
    if (-not (Test-Path $t.Patch)) { throw "Missing patch file: $($t.Patch)" }
    if (-not (Test-Path $t.Repo))  { throw "Missing dependency: $($t.Repo). Run fetch_deps.ps1." }

    $name = $t.Name
    if (Test-GitApply -Repo $t.Repo -Patch $t.Patch -Reverse) {
        Write-Host "$name patch already applied."
        continue
    }

    if (Test-GitApply -Repo $t.Repo -Patch $t.Patch) {
        & git -C $t.Repo apply $t.Patch
        if ($LASTEXITCODE -ne 0) { throw "Failed to apply $($t.Patch) to $($t.Repo)." }
        Write-Host "Applied $name patch."
        continue
    }

    throw "$name in $($t.Repo) is neither pristine nor patched; inspect it with git status."
}
