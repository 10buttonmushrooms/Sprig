#requires -Version 5.1
<#
.SYNOPSIS
  Downloads PvZH Altrox v1.12.2 and stages its bundles for the Altrox APK.

.DESCRIPTION
  build_mod.ps1 -Apk Altrox runs this first. The mod's files/ folder mirrors the
  game's cache/bundles/files/ and becomes bundles/. The helper's 50 HP addon,
  data_assets_16, moves to fifty-hp/ so config.ini can choose it.
#>

$ErrorActionPreference = 'Stop'

$Url     = 'https://drive.usercontent.google.com/download?id=1LoZ1iuU4K887mbwSUyAvQP5PDyZ4fbD9&export=download&confirm=t'
$Sha256  = '271227639626a64bff44f7aeb8ddd7ef6c272cf469e78fc6e733f1226b84e6bb'
$Bundles = Join-Path $PSScriptRoot 'bundles'
$FiftyHp = Join-Path $PSScriptRoot 'fifty-hp'
$Stamp   = Join-Path $PSScriptRoot '.setup'

if ((Test-Path $Stamp) -and ((Get-Content -Raw $Stamp).Trim() -eq $Sha256) -and
    (Test-Path (Join-Path $Bundles 'cards')) -and (Test-Path (Join-Path $FiftyHp 'data_assets_16'))) {
    Write-Host 'Altrox bundles are ready.'
    return
}

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Downloads   = Join-Path $ProjectRoot 'work\build-out\downloads'
$Zip         = Join-Path $Downloads 'PvZH-Altrox-v1.12.2.zip'
$Extract     = Join-Path $Downloads 'altrox'
New-Item -ItemType Directory -Force -Path $Downloads | Out-Null

if (-not (Test-Path $Zip) -or (Get-FileHash $Zip -Algorithm SHA256).Hash -ine $Sha256) {
    Write-Host 'Downloading PvZH Altrox v1.12.2 (138 MB)...'
    $oldProgress = $ProgressPreference
    try {
        # Windows PowerShell 5.1 downloads large files very slowly while drawing progress.
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest $Url -OutFile $Zip -UseBasicParsing
    } finally {
        $ProgressPreference = $oldProgress
    }
    if ((Get-FileHash $Zip -Algorithm SHA256).Hash -ine $Sha256) {
        Remove-Item -LiteralPath $Zip -Force
        throw 'Altrox download checksum failed.'
    }
}

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
