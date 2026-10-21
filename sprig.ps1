#requires -Version 5.1
# Sprig's tools for writing mods. Run .\sprig.ps1 help (or sprig.cmd help from cmd or Git Bash).
# Arguments are read from $args so that flags such as --all pass through unchanged.
$command = if ($args.Count -gt 0) { [string]$args[0] } else { 'help' }
# @(...) keeps a one-element list an array; an if-expression would unroll it into a string,
# and splatting a string passes its characters.
$rest = @($args | Select-Object -Skip 1 | ForEach-Object { [string]$_ })
$scripts = Join-Path $PSScriptRoot 'work\scripts\sprig'

switch ($command) {
    'find' { & (Join-Path $scripts 'find.ps1') @rest; exit $LASTEXITCODE }
    'lint' { & (Join-Path $scripts 'lint.ps1') @rest; exit $LASTEXITCODE }
    default {
        @(
            'Sprig tools (they read work\dump\dump.cs):'
            ''
            '  .\sprig.ps1 find <Type | Namespace.Type | Type.Member> [--all]'
            '      A type''s fields with offsets, methods with RVAs and virtual slots, the BNM lookup,'
            '      and for a method the exact hook signature. Wrong names get the closest real ones.'
            '  .\sprig.ps1 find --derives <Type>     every type that derives from or implements Type'
            '  .\sprig.ps1 find --rva <0xADDRESS>    the method containing an address (crash mapping)'
            '  .\sprig.ps1 lint [Module...]          check modules against dump.cs (build_mod.ps1 runs it)'
            ''
            'From cmd or Git Bash use sprig.cmd instead of .\sprig.ps1. Start with AGENTS.md.'
        ) | Write-Output
        exit $(if ($command -in 'help', '-h', '--help') { 0 } else { 2 })
    }
}
