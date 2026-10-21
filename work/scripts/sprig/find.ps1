#requires -Version 5.1
# sprig find <Type | Namespace.Type | Type.Member> [--all] | --derives <Type> | --rva <0xADDRESS>
# Arguments are read from $args so that flags such as --all pass through unchanged.
# Capture them first: dot-sourcing a script replaces $args.
$argv = [string[]]@($args)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

if (-not (Test-SprigDump)) {
    Write-Output "work\dump\dump.cs not found. Put the Il2CppDumper dump.cs for input\Game.apk at $SprigDump."
    exit 2
}
$index = Get-SprigIndex
$output = New-Object System.Text.StringBuilder
$code = [Sprig.FindCommand]::Run($index, $argv, $output)
Write-Output $output.ToString().TrimEnd()
exit $code
