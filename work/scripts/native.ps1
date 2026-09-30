#requires -Version 5.1
function Invoke-Native {
    # No named parameters: native flags such as apktool's -f must stay arguments.
    $executable, $nativeArgs = $args
    $oldPreference = $ErrorActionPreference
    try {
        # PS 5.1 treats redirected native stderr as an error, even on success.
        # Keep diagnostics, and let the process exit code decide success.
        $ErrorActionPreference = 'Continue'
        & $executable @nativeArgs
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldPreference
    }
    if ($code -ne 0) { throw "$executable failed (exit $code)." }
}
