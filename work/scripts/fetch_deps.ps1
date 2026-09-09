#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path

$dependencies = @(
    @{ Name = 'BNM-Android'; Url = 'https://github.com/ByNameModding/BNM-Android.git'; Commit = '502928771983d29e37f28c78f28823dfb775a3aa' },
    @{ Name = 'Dobby'; Url = 'https://github.com/jmpews/Dobby.git'; Commit = '5dfc8546954ce3b3198132ab13fddb89ee92cdd7' }
)

foreach ($dependency in $dependencies) {
    $path = Join-Path $ProjectRoot "deps\$($dependency.Name)"
    $gitDir = Join-Path $path '.git'

    if (-not (Test-Path $gitDir)) {
        if ((Test-Path $path) -and (Get-ChildItem $path -Force)) {
            throw "$path is not empty and is not a Git checkout."
        }
        New-Item -ItemType Directory -Force $path | Out-Null
        & git -C $path init --quiet
        & git -C $path remote add origin $dependency.Url
    }

    $head = (& git -C $path rev-parse HEAD 2>$null)
    if ($head -ne $dependency.Commit) {
        if (& git -C $path status --porcelain) {
            throw "$($dependency.Name) has local changes at the wrong revision."
        }
        & git -C $path fetch --depth 1 origin $dependency.Commit
        if ($LASTEXITCODE -ne 0) { throw "Failed to fetch $($dependency.Name)." }
        & git -C $path checkout --quiet --detach FETCH_HEAD
        if ($LASTEXITCODE -ne 0) { throw "Failed to check out $($dependency.Name)." }
    }

    Write-Host "$($dependency.Name) $($dependency.Commit)"
}
