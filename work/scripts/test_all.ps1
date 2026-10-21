#requires -Version 5.1
$ErrorActionPreference = 'Stop'

& "$PSScriptRoot\test_pipeline.ps1"
& "$PSScriptRoot\test_bloom.ps1"
& "$PSScriptRoot\test_sprig_tools.ps1"
