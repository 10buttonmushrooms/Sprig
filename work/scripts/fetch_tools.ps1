#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot\..\..").Path
$tools = @(
    @{ Path = 'tools\apktool\apktool_3.0.2.jar'; Url = 'https://github.com/iBotPeaches/Apktool/releases/download/v3.0.2/apktool_3.0.2.jar'; Sha = 'eee4669a704a14e0623407e6701b0b91887e61e1e4049cb7a82833e14ae8b5fd' },
    @{ Path = 'tools\uber-apk-signer\uber-apk-signer-1.3.0.jar'; Url = 'https://github.com/patrickfav/uber-apk-signer/releases/download/v1.3.0/uber-apk-signer-1.3.0.jar'; Sha = 'e1299fd6fcf4da527dd53735b56127e8ea922a321128123b9c32d619bba1d835' }
)

foreach ($tool in $tools) {
    $path = Join-Path $root $tool.Path
    if ((Test-Path $path) -and (Get-FileHash $path -Algorithm SHA256).Hash -ieq $tool.Sha) { continue }
    New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
    Invoke-WebRequest $tool.Url -OutFile $path -UseBasicParsing
    if ((Get-FileHash $path -Algorithm SHA256).Hash -ine $tool.Sha) {
        Remove-Item $path -Force
        throw "Checksum failed: $($tool.Path)"
    }
}
