#requires -Version 5.1
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\native.ps1"
$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-bloom-$([guid]::NewGuid())"
function Write-Fixture {
    param([string]$Relative, [string]$Text)
    $path = Join-Path $sandbox $Relative
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    [System.IO.File]::WriteAllText($path, $Text, [System.Text.UTF8Encoding]::new($false))
}
function Assert-Bloom {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}
try {
    Assert-Bloom (Test-Path "$ProjectRoot\mod\cmake\Bloom.cmake") 'Bloom discovery is missing.'
    New-Item -ItemType Directory -Force "$sandbox\mod" | Out-Null
    Copy-Item "$ProjectRoot\mod\cmake", "$ProjectRoot\mod\include", "$ProjectRoot\mod\src" "$sandbox\mod" -Recurse
    New-Item -ItemType Directory -Force "$sandbox\Bloom" | Out-Null
    Copy-Item "$ProjectRoot\Bloom\_templates" "$sandbox\Bloom\_templates" -Recurse -Force
    Write-Fixture 'mod/CMakeLists.txt' @'
cmake_minimum_required(VERSION 3.22)
project(BloomCheck CXX)
set(CMAKE_CXX_STANDARD 20)
add_executable(sprig check.cpp src/bloom.cpp)
target_include_directories(sprig PRIVATE include)
include(cmake/Bloom.cmake)
if(BLOOM_APK)
    target_compile_definitions(sprig PRIVATE EXPECT_PHONE)
endif()
'@
    Write-Fixture 'mod/check.cpp' @'
#include <bloom/bloom.h>
#include <cassert>
#include <thread>
#include "core.h"
int calls = 0;
int phone_calls = 0;
int main() {
#ifdef EXPECT_PHONE
    assert(Bloom::Modules().size() == 4 && Bloom::FindModule("Phone"));
#else
    assert(Bloom::Modules().size() == 3 && !Bloom::FindModule("Phone"));
#endif
    assert(Bloom::FindModule("Library")->type == Bloom::Type::Core);
    assert(!Bloom::FindModule("Invalid"));
    assert(!Bloom::FindModule("Core")); // templates must not install themselves
    assert(core_value() == 42 && core_main() == 7 && header_value() == 9);
    assert(calls == 0);
    Bloom::InitializePnP();
    std::thread again([] { Bloom::InitializePnP(); });
    again.join();
    Bloom::InitializePnP();
    assert(calls == 1);
#ifdef EXPECT_PHONE
    assert(phone_calls == 1);
#else
    assert(phone_calls == 0);
#endif
}
'@
    Write-Fixture 'Bloom/Library/bloom.ini' "[Other]`nType=PnP`n[Bloom]`nType=Core`n"
    Write-Fixture 'Bloom/Library/inc/core.h' "#pragma once`nint core_value();`nint core_main();`n#include <header.h>`n"
    Write-Fixture 'Bloom/Library/src/nested/core.cxx' ('#include "core.h"' + "`n" + 'int core_value() { return 42; }')
    Write-Fixture 'Bloom/Library/main.cpp' @'
#include <bloom/bloom.h>
#include <string_view>
static_assert(std::string_view(BLOOM_MODULE_NAME) == "Library");
static_assert(BLOOM_MODULE_TYPE == Bloom::Type::Core);
int core_main() { return 7; }
'@
    Write-Fixture 'Bloom/Headers/bloom.ini' "[Bloom]`nType=Core`n"
    Write-Fixture 'Bloom/Headers/include/header.h' "#pragma once`ninline int header_value() { return 9; }`n"
    Write-Fixture 'Bloom/Plugin/bloom.ini' ([string][char]0xFEFF + "[Bloom]`r`nType = PnP ; module`r`n")
    Write-Fixture 'Bloom/Plugin/config.ini' "[General]`r`nEnabled = true`r`nPriority = -12`r`nBadInt = 12junk`r`nOverflow = 9999999999999999999999`r`nBadBool = maybe`r`nEmpty = `r`nText = a; b # c = d `"quoted`"`r`n"
    Write-Fixture 'Bloom/Plugin/main.cpp' @'
#include <bloom/bloom.h>
#include <cassert>
#include "core.h"
extern int calls;
static void Init() {
    ++calls;
    assert(core_value() == 42);
    assert(BLOOM_MODULE_TYPE == Bloom::Type::PnP);
    assert(BLOOM_CONFIG_BOOL("General", "Enabled", false));
    assert(BLOOM_CONFIG_INT("General", "Priority", 0) == -12);
    assert(BLOOM_CONFIG_INT("General", "BadInt", 4) == 4);
    assert(BLOOM_CONFIG_INT("General", "Overflow", 5) == 5);
    assert(BLOOM_CONFIG_BOOL("General", "BadBool", true));
    assert(BLOOM_CONFIG("General", "Missing").empty());
    assert(BLOOM_CONFIG("General", "Empty").empty());
    assert(BLOOM_CONFIG("General", "Text") == "a; b # c = d \"quoted\"");
    Bloom::InitializePnP(); // callback re-entry must neither deadlock nor repeat
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Plugin/bundles/nested/example.bin' 'bundle'
    Write-Fixture 'Bloom/Plugin/textures/icon.txt' 'texture'
    Write-Fixture 'Bloom/Invalid/bloom.ini' "[Other]`nType=PnP`n"
    Write-Fixture 'Bloom/Invalid/main.cpp' '#error Invalid modules must be skipped'
    Write-Fixture 'Bloom/Invalid/bundles/bad.bin' 'bad'
    Write-Fixture 'Bloom/Phone/bloom.ini' "[Bloom]`nType=PnP`n[Apk]`nLabel=Phone`n"
    Write-Fixture 'Bloom/Phone/main.cpp' @'
#include <bloom/bloom.h>
#ifndef EXPECT_PHONE
#error APK modules must stay out of other builds
#endif
extern int phone_calls;
static void Init() { ++phone_calls; }
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Phone/bundles/phone.bin' 'phone'
    Invoke-Native cmake -S "$sandbox\mod" -B "$sandbox\build" -G 'Visual Studio 17 2022' -A x64
    Invoke-Native cmake --build "$sandbox\build" --config Debug
    Invoke-Native "$sandbox\build\Debug\sprig.exe"
    Assert-Bloom (-not (Test-Path "$sandbox\build\bloom-assets\Phone")) 'APK module assets must stay out of the default build.'
    Assert-Bloom (Test-Path "$sandbox\build\bloom-assets\Plugin\bundles\nested\example.bin") 'Nested bundles were not staged.'
    Assert-Bloom (Test-Path "$sandbox\build\bloom-assets\Plugin\textures\icon.txt") 'Arbitrary asset directories were not staged.'
    Assert-Bloom (-not (Test-Path "$sandbox\build\bloom-assets\Plugin\src")) 'Code directories must not be staged.'
    Assert-Bloom (-not (Test-Path "$sandbox\build\bloom-assets\Invalid")) 'Invalid module assets must not be staged.'

    # Config content and removed/new assets must update on cmake --build alone.
    $config = Get-Content -Raw "$sandbox\Bloom\Plugin\config.ini"
    Write-Fixture 'Bloom/Plugin/config.ini' ($config.Replace('Priority = -12', 'Priority = +23'))
    $plugin = Get-Content -Raw "$sandbox\Bloom\Plugin\main.cpp"
    Write-Fixture 'Bloom/Plugin/main.cpp' ($plugin.Replace('== -12', '== 23'))
    Remove-Item -LiteralPath "$sandbox\Bloom\Plugin\textures\icon.txt"
    Write-Fixture 'Bloom/Plugin/bundles/new.bin' 'new'
    Invoke-Native cmake --build "$sandbox\build" --config Debug
    Invoke-Native "$sandbox\build\Debug\sprig.exe"
    Assert-Bloom (-not (Test-Path "$sandbox\build\bloom-assets\Plugin\textures\icon.txt")) 'Removed assets linger in staging.'
    Assert-Bloom (Test-Path "$sandbox\build\bloom-assets\Plugin\bundles\new.bin") 'New assets were not discovered.'

    # -DBLOOM_APK adds exactly that [Apk] module; an unknown one is a configure error.
    Invoke-Native cmake -S "$sandbox\mod" -B "$sandbox\build-phone" -G 'Visual Studio 17 2022' -A x64 '-DBLOOM_APK=Phone'
    Invoke-Native cmake --build "$sandbox\build-phone" --config Debug
    Invoke-Native "$sandbox\build-phone\Debug\sprig.exe"
    Assert-Bloom (Test-Path "$sandbox\build-phone\bloom-assets\Phone\bundles\phone.bin") 'APK module assets were not staged in their APK build.'
    $missingApk = $null
    try { Invoke-Native cmake -S "$sandbox\mod" -B "$sandbox\build-missing" -G 'Visual Studio 17 2022' -A x64 '-DBLOOM_APK=Missing' 2>&1 | Out-Null }
    catch { $missingApk = $_.Exception.Message }
    Assert-Bloom ([bool]$missingApk) 'An unknown BLOOM_APK must fail configuration.'
    Write-Host 'All Bloom integration checks passed.'
    # The expected configure failure must not become this script's exit code.
    $global:LASTEXITCODE = 0
} finally {
    # The target is the literal, uniquely named temp directory created above.
    if (Test-Path $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}
