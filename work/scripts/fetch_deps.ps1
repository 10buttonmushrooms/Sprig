#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path

$dependencies = @(
    @{ Name = 'BNM-Android'; Url = 'https://github.com/ByNameModding/BNM-Android.git'; Commit = '502928771983d29e37f28c78f28823dfb775a3aa' },
    @{ Name = 'Dobby'; Url = 'https://github.com/jmpews/Dobby.git'; Commit = '5dfc8546954ce3b3198132ab13fddb89ee92cdd7' }
)

function Invoke-GitProbe {
    param([string[]]$GitArgs)

    $oldPreference = $ErrorActionPreference
    try {
        # Windows PowerShell 5.1 can promote redirected native stderr to a
        # terminating NativeCommandError when ErrorActionPreference is Stop.
        # Probes intentionally use exit codes, so let Git fail normally here.
        $ErrorActionPreference = 'Continue'
        $output = @(& git @GitArgs 2>$null)
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldPreference
    }

    return [pscustomobject]@{ ExitCode = $code; Output = $output }
}

foreach ($dependency in $dependencies) {
    $path = Join-Path $ProjectRoot "deps\$($dependency.Name)"
    $gitDir = Join-Path $path '.git'

    if (-not (Test-Path $gitDir)) {
        if ((Test-Path $path) -and (Get-ChildItem $path -Force)) {
            throw "$path is not empty and is not a Git checkout."
        }

        New-Item -ItemType Directory -Force $path | Out-Null
        & git -C $path init --quiet
        if ($LASTEXITCODE -ne 0) { throw "Failed to initialize $($dependency.Name)." }
    }

    # Generated dependency checkouts must keep upstream LF bytes so the pinned
    # compatibility patches apply identically even when global core.autocrlf is true.
    & git -C $path config core.autocrlf false
    if ($LASTEXITCODE -ne 0) { throw "Failed to configure line endings for $($dependency.Name)." }

    $originProbe = Invoke-GitProbe -GitArgs @('-C', $path, 'remote', 'get-url', 'origin')
    if ($originProbe.ExitCode -ne 0) {
        & git -C $path remote add origin $dependency.Url
        if ($LASTEXITCODE -ne 0) { throw "Failed to add origin for $($dependency.Name)." }
    } else {
        $origin = [string]($originProbe.Output | Select-Object -First 1)
        if ($origin -ne $dependency.Url) {
            & git -C $path remote set-url origin $dependency.Url
            if ($LASTEXITCODE -ne 0) { throw "Failed to repair origin for $($dependency.Name)." }
        }
    }

    # A freshly initialized repository has an unborn HEAD. Probe it without
    # letting the expected nonzero exit become a PowerShell exception.
    $headProbe = Invoke-GitProbe -GitArgs @('-C', $path, 'rev-parse', '--verify', 'HEAD')
    $head = if ($headProbe.ExitCode -eq 0) { [string]($headProbe.Output | Select-Object -First 1) } else { $null }

    if ($head -ne $dependency.Commit) {
        $statusProbe = Invoke-GitProbe -GitArgs @('-C', $path, 'status', '--porcelain')
        if ($statusProbe.ExitCode -ne 0) { throw "Failed to inspect $($dependency.Name)." }
        if ($statusProbe.Output.Count -gt 0) {
            throw "$($dependency.Name) has local changes at the wrong revision."
        }

        & git -C $path fetch --depth 1 origin $dependency.Commit
        if ($LASTEXITCODE -ne 0) { throw "Failed to fetch $($dependency.Name)." }

        & git -C $path checkout --quiet --detach FETCH_HEAD
        if ($LASTEXITCODE -ne 0) { throw "Failed to check out $($dependency.Name)." }
    }

    Write-Host "$($dependency.Name) $($dependency.Commit)"
}
