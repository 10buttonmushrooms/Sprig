#requires -Version 5.1
<#
.SYNOPSIS
  Stages the bundles of the PvZH Altrox beta for the Altrox APK.

.DESCRIPTION
  build.ps1 runs this first. The mod's files/ folder mirrors the game's
  cache/bundles/files/ and becomes module\bundles\. The helper's 50 HP addon,
  data_assets_16, moves to module\fifty-hp\ so config.ini can choose it.

  The beta has no public download link: it is copied from -Zip into
  work\build-out\downloads\ the first time.

.PARAMETER Zip
  Path of 'PvZH Altrox Beta.zip'. Defaults to that file in your Downloads folder.
#>
param([string]$Zip = (Join-Path $env:USERPROFILE 'Downloads\PvZH Altrox Beta.zip'))

$ErrorActionPreference = 'Stop'

$Sha256  = '14b0f16c7135f4ea991770dc31d921fb81b62034c161c42be13735ad8a304156'
$Module  = Join-Path $PSScriptRoot 'module'
$Bundles = Join-Path $Module 'bundles'
$FiftyHp = Join-Path $Module 'fifty-hp'
$Stamp   = Join-Path $Module '.setup'

if ((Test-Path $Stamp) -and ((Get-Content -Raw $Stamp).Trim() -eq $Sha256) -and
    (Test-Path (Join-Path $Bundles 'cards')) -and (Test-Path (Join-Path $FiftyHp 'data_assets_16'))) {
    Write-Host 'Altrox bundles are ready.'
    return
}

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Downloads   = Join-Path $ProjectRoot 'work\build-out\downloads'
$Cached      = Join-Path $Downloads 'PvZH-Altrox-Beta.zip'
$Extract     = Join-Path $Downloads 'altrox'
New-Item -ItemType Directory -Force -Path $Downloads | Out-Null

if (-not (Test-Path $Cached) -or (Get-FileHash $Cached -Algorithm SHA256).Hash -ine $Sha256) {
    if (-not (Test-Path -LiteralPath $Zip)) {
        throw "Missing the Altrox beta. Run: .\Bloom\Altrox\setup.ps1 -Zip '<path to PvZH Altrox Beta.zip>'"
    }
    if ((Get-FileHash -LiteralPath $Zip -Algorithm SHA256).Hash -ine $Sha256) {
        throw "$Zip is not the expected Altrox beta (SHA-256 mismatch)."
    }
    Copy-Item -LiteralPath $Zip -Destination $Cached -Force
}
$Zip = $Cached

Write-Host 'Extracting Altrox bundles...'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path $Extract) { Remove-Item -LiteralPath $Extract -Recurse -Force }
[System.IO.Compression.ZipFile]::ExtractToDirectory($Zip, $Extract)

foreach ($dir in @($Bundles, $FiftyHp)) {
    if (Test-Path $dir) { Remove-Item -LiteralPath $dir -Recurse -Force }
}
Move-Item -LiteralPath (Join-Path $Extract 'files') -Destination $Bundles
New-Item -ItemType Directory -Force -Path $FiftyHp | Out-Null
Move-Item -LiteralPath (Join-Path $Bundles 'data_assets_16') -Destination $FiftyHp
Remove-Item -LiteralPath $Extract -Recurse -Force

[System.IO.File]::WriteAllText($Stamp, $Sha256, [System.Text.UTF8Encoding]::new($false))
Write-Host ("Staged {0} Altrox bundles." -f @(Get-ChildItem $Bundles -Recurse -File).Count)
