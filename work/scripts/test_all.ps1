#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-all-$([guid]::NewGuid())"
$scripts = Join-Path $sandbox 'work\scripts'
$apk = Join-Path $sandbox 'input\Game.apk'

try {
    New-Item -ItemType Directory -Force $scripts | Out-Null
    Copy-Item "$PSScriptRoot\all.ps1" $scripts
    'fetch_tools', 'build_mod', 'inject_mod', 'install' | ForEach-Object {
        Set-Content (Join-Path $scripts "$_.ps1") ''
    }

    function Invoke-WebRequest {
        param([string]$Uri, [string]$OutFile, [switch]$UseBasicParsing)
        if ($Uri -notlike 'https://cdn.discordapp.com/*') { throw "Unexpected URL: $Uri" }
        New-Item -ItemType Directory -Force (Split-Path $OutFile) | Out-Null
        Set-Content $OutFile 'apk'
    }

    & (Join-Path $scripts 'all.ps1')
    if (-not (Test-Path $apk)) { throw 'all.ps1 did not download a missing Game.apk' }
} finally {
    Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue
}
