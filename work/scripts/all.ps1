#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Apk = Join-Path $ProjectRoot 'input\Game.apk'
if (-not (Test-Path $Apk)) {
    Invoke-WebRequest 'https://cdn.discordapp.com/attachments/1132006840813756544/1547322552811131031/Game.apk?ex=6aa2ffec&is=6aa1ae6c&hm=04b0f14e423ff9eb826733b7b53bda0508e58bb74d856cbed3084916231a72d2&' -OutFile $Apk -UseBasicParsing
}

& "$PSScriptRoot\fetch_tools.ps1"
& "$PSScriptRoot\build_mod.ps1"
& "$PSScriptRoot\inject_mod.ps1"
& "$PSScriptRoot\install.ps1"
