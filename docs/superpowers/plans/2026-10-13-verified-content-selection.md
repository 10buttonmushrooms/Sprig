# Verified Content Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Sprig's "always pick the newest content" hook with selection of verified content only, restoring the verified association if EA drops it and repairing devices left on unverified content.

**Architecture:** Pure C++ selection rules (`mod/src/content/policy.cpp`) are tested on the host. A CMake-embedded content record (`mod/content/`) is the single source of truth for the executable and the verified content version. Three BNM hooks (`mod/src/content/hooks.cpp`) apply the rules inside the game's map download handler. PowerShell tooling adds a build guard, device acceptance cases, map capture, and verification of newer content.

**Tech Stack:** C++20, BNM-Android v2.5.2 with Dobby, CMake with Ninja (NDK) and Visual Studio 2022 (host tests), Windows PowerShell 5.1 and PowerShell 7, adb.

**Spec:** `docs/superpowers/specs/2026-10-13-verified-content-selection-design.md`

## Global Constraints

- The executable stays PvZH 1.65.27 (`input/Game.apk`). Verified content ceiling: asset version `1.67.8`.
- Runtime never selects an asset version above the record's `AssetVersion`.
- Every call from a hook into managed code is wrapped in `BNM::TryInvoke`. On any failure, fall back to the original method's result or skip the optional step.
- The record lives in `mod/content/target.ini` (`[Executable] AppVersion`, `[Content] AssetVersion`, `[Verification] SuccessMarker`, `[Verification] FailurePattern`) and `mod/content/association.txt` (byte-exact apart from a dropped UTF-8 BOM).
- Versions are one to four dot-separated decimal components, each from 0 to 2147483647. Surrounding whitespace is ignored and missing components compare as zero.
- All scripts must parse and run in Windows PowerShell 5.1 and PowerShell 7 (`test_pipeline.ps1` enforces parsing). Native calls go through `Invoke-Native` from `work/scripts/native.ps1`.
- Log tag stays `Sprig` (`mod/include/log.h`). The exact log lines are listed in Task 4 and are relied on by Tasks 6 and 7.
- Device steps use the package `com.ea.gp.pvzheroes` and the activity `com.ea.gp.pvzheroes/com.ea.nimble.plugin.NimbleActivity`.

## Review Focus

- Duplicate entries for the target in the live map, for example EA republishing 1.67.8 with a new `ServerVersion`. Selection must resolve to the first in map order, deterministically. Test: Task 2.
- Textually different but numerically equal versions (`1.67.8.0`, ` 1.67.8`) must count as the target: selected, not restored, no reset. Tests: Task 2.
- `association.txt` saved by a Windows editor with a UTF-8 BOM or CRLF. The BOM must be dropped and CRLF kept, because the bytes go to the game's parser. Test: Task 3.
- A target component above int32 in `target.ini` (for example `1.67.2147483648`) must fail at configure time, not ship a target the runtime cannot parse. Test: Task 3.
- `input/Game.apk` swapped for another version (for example 1.66.7) must stop `inject_mod.ps1` before repacking. Test: Task 5.

---

## Before you start

The working tree already has uncommitted owner changes: `work/scripts/test_all.ps1`, `work/scripts/test_pipeline.ps1`, and the new `work/scripts/test_bloom.ps1`. Ask the owner to commit or stash them first. Do not include them in this plan's commits. Tasks 2, 3, 4 and 5 edit `test_all.ps1` and `test_pipeline.ps1`. Add only the lines shown, on top of whatever the owner commits.

Tasks 1, 4 and 6 need the phone connected (`adb devices` shows `device`) and `ANDROID_NDK_ROOT` set.

## File map

| Path | Status | Responsibility |
| --- | --- | --- |
| `mod/content/target.ini` | create (T1, T4) | Executable and verified content version, plus verification markers |
| `mod/content/association.txt` | create (T1) | Minimal live-map document containing only the target association |
| `mod/include/content/policy.h`, `mod/src/content/policy.cpp` | create (T2) | Pure version and selection rules |
| `mod/cmake/ContentRecord.cmake` | create (T3) | Load and validate the record; script-mode safe |
| `mod/cmake/Content.cmake` | create (T3) | Embed the record, write the guard stamp, debug options |
| `mod/include/content/record.h` | create (T3) | `Content::Record` and `Content::GetRecord()` |
| `mod/include/content/hooks.h`, `mod/src/content/hooks.cpp` | create (T4) | BNM hooks: Read, FindAssociation, HandleDownloadRequestSuccess |
| `mod/src/main.cpp` | modify (T4) | Drop `UseLatestContent`; call `Content::InstallHooks()` |
| `mod/include/log.h` | modify (T4) | Add `LOG_FATAL` |
| `mod/CMakeLists.txt` | modify (T3, T4) | Content sources and `Content.cmake` |
| `work/scripts/build_mod.ps1` | modify (T3) | `-ContentDir`, `-DropTarget`, `-LogContentMap`, always passed to CMake |
| `work/scripts/content_guard.ps1` | create (T5) | `Assert-ContentExecutable` |
| `work/scripts/inject_mod.ps1` | modify (T5) | Call the guard after unpacking |
| `work/scripts/device.ps1` | create (T1), extend (T6) | adb helpers shared by acceptance, capture, and verify |
| `work/scripts/test_content.ps1` | create (T2), extend (T3, T5) | Host checks: policy, record, guard |
| `work/scripts/test_device_content.ps1` | create (T6) | Device acceptance cases A-F |
| `work/scripts/capture_content_map.ps1`, `work/scripts/verify_content.ps1` | create (T7) | Capture the live map; trial newer content |
| `.github/workflows/windows-tests.yml` | modify (T2) | Run `test_content.ps1` in CI |
| `.gitignore`, `.gitattributes` | modify (T1) | Ignore device logs and map captures; keep `association.txt` byte-exact |
| `README.md`, `docs/getting-started.md`, `docs/setup.md` | modify (T8) | Describe the new behavior |

---

### Task 1: Capture device evidence and the content record

Throwaway diagnostics run on the current behavior, which still picks the newest content. They capture EA's live map, the tracker call order on the stuck path, and the failure screen marker. Only data, `device.ps1`, ignore rules and the spec update are committed. The diagnostic patch is reverted.

**Files:**
- Create: `work/scripts/device.ps1`, `mod/content/target.ini`, `mod/content/association.txt`
- Modify: `.gitignore`, `.gitattributes`, `docs/superpowers/specs/2026-10-13-verified-content-selection-design.md` (Evidence section)
- Temporary (reverted in Step 9): `mod/src/main.cpp`

**Interfaces:**
- Produces: `device.ps1` with `$SprigPackage`, `$SprigActivity`, `$SprigCrashPattern`, `Start-SprigGame`, `Watch-Logcat -LogPath -Classify -TimeoutSeconds`, `Wait-LogPattern -LogPath -Pattern -TimeoutSeconds`, `ConvertFrom-ContentMapLog -LogPath` and `Get-ContentSetting -Key`. The map chunk log format is `content: map text begin (N bytes)`, `content: map NNNN <escaped payload>` and `content: map text end`, with `\\`, `\n` and `\r` escaped. Task 4 emits the same format.
- Produces: `work/build-out/Sprig-newest-content.apk`, the current broken build, used by acceptance case B.

- [ ] **Step 1: Keep a copy of the broken build for acceptance case B**

```powershell
Copy-Item .\out\Sprig.apk .\work\build-out\Sprig-newest-content.apk
```

If `out\Sprig.apk` is missing, run `.\work\scripts\build_mod.ps1; .\work\scripts\inject_mod.ps1` on the unmodified tree first, then copy.

- [ ] **Step 2: Ignore device artifacts and keep the association byte-exact**

Append to `.gitignore`:

```text
work/device-logs/
work/content-map.txt
```

Append to `.gitattributes`:

```text
mod/content/association.txt -text
```

- [ ] **Step 3: Create `work/scripts/device.ps1`**

```powershell
#requires -Version 5.1
# Shared adb helpers for on-device content checks. Dot-source this file; it only defines names.
. "$PSScriptRoot\native.ps1"

$SprigPackage = 'com.ea.gp.pvzheroes'
$SprigActivity = 'com.ea.gp.pvzheroes/com.ea.nimble.plugin.NimbleActivity'
# Android truncates the process name to its last 15 characters in fatal-signal lines.
$SprigCrashPattern = 'Fatal signal .*\(ea\.gp\.pvzheroes\)|Process: com\.ea\.gp\.pvzheroes, PID|ANR in com\.ea\.gp\.pvzheroes'

function Get-ContentSetting {
    param([Parameter(Mandatory)][string]$Key)
    $ini = Get-Content -Raw (Join-Path $PSScriptRoot '..\..\mod\content\target.ini')
    if ($ini -notmatch "(?m)^[ \t]*$([regex]::Escape($Key))[ \t]*=[ \t]*([^\r\n]+?)[ \t]*$") {
        throw "$Key is missing from mod\content\target.ini."
    }
    return $Matches[1]
}

function Start-SprigGame {
    Invoke-Native adb shell am force-stop $SprigPackage
    Invoke-Native adb logcat -c
    Invoke-Native adb shell am start -n $SprigActivity | Out-Null
}

# Streams logcat to $LogPath and returns the first non-empty result of $Classify for the text so far.
function Watch-Logcat {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][scriptblock]$Classify,
        [int]$TimeoutSeconds = 300
    )
    New-Item -ItemType Directory -Force (Split-Path $LogPath) | Out-Null
    $logcat = Start-Process adb -ArgumentList 'logcat', '-v', 'threadtime' -RedirectStandardOutput $LogPath -NoNewWindow -PassThru
    try {
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 2
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            if ($text) {
                $result = & $Classify $text
                if ($result) { return $result }
            }
            if ($logcat.HasExited) { return 'Disconnected' }
        }
        return 'Timeout'
    } finally {
        if (-not $logcat.HasExited) { Stop-Process -Id $logcat.Id -Force }
    }
}

function Wait-LogPattern {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][string]$Pattern,
        [int]$TimeoutSeconds = 300
    )
    $outcome = Watch-Logcat -LogPath $LogPath -TimeoutSeconds $TimeoutSeconds -Classify {
        param($text)
        if ($text -match $Pattern) { 'Found' }
    }.GetNewClosure()
    if ($outcome -ne 'Found') { throw "Did not see '$Pattern' in logcat ($outcome). Log: $LogPath" }
}

# Reassembles the first map logged after 'content: map text begin'.
function ConvertFrom-ContentMapLog {
    param([Parameter(Mandatory)][string]$LogPath)
    $parts = New-Object System.Collections.Generic.List[string]
    $reading = $false
    foreach ($line in Get-Content -LiteralPath $LogPath -Encoding UTF8) {
        if ($line -match 'content: map text begin') { $parts.Clear(); $reading = $true; continue }
        if ($reading -and $line -match 'content: map text end') { break }
        if ($reading -and $line -match 'content: map (\d{4}) (.*)$') { $parts.Add($Matches[2]) }
    }
    if ($parts.Count -eq 0) { throw "No content map found in $LogPath." }
    return [regex]::Replace((-join $parts), '\\(.)', {
        param($match)
        switch ($match.Groups[1].Value) { 'n' { "`n" } 'r' { "`r" } default { $match.Groups[1].Value } }
    })
}
```

- [ ] **Step 4: Apply the throwaway diagnostic patch to `mod/src/main.cpp`**

Insert this block directly above `static void OnIl2CppLoaded() {`:

```cpp
// ---- Task 1 diagnostics. Revert with: git checkout -- mod/src/main.cpp ----
#include <string>

using DiagObject = BNM::IL2CPP::Il2CppObject;
using DiagString = BNM::Structures::Mono::String;

static std::string DiagText(DiagString *value) { return value ? value->str() : std::string{}; }

static void DiagLogMap(const std::string &text) {
    std::string escaped;
    for (char c : text) {
        if (c == '\\') escaped += "\\\\";
        else if (c == '\n') escaped += "\\n";
        else if (c == '\r') escaped += "\\r";
        else escaped += c;
    }
    LOG_INFO("content: map text begin (%zu bytes)", text.size());
    for (std::size_t at = 0, part = 0; at < escaped.size(); at += 800, ++part)
        LOG_INFO("content: map %04zu %s", part, escaped.substr(at, 800).c_str());
    LOG_INFO("content: map text end");
}

using DiagReadFn = DiagObject *(*)(DiagString *, DiagObject *, const void *);
using DiagArgFn = void (*)(DiagObject *, DiagObject *, const void *);
using DiagVoidFn = void (*)(DiagObject *, const void *);
using DiagStringFn = DiagString *(*)(DiagObject *, const void *);
using DiagBoolFn = bool (*)(DiagObject *, const void *);

static DiagReadFn diagRead = nullptr;
static DiagArgFn diagSuccess = nullptr, diagOffline = nullptr;
static DiagVoidFn diagRecord = nullptr, diagClear = nullptr;
static DiagStringFn diagLastKnown = nullptr;
static DiagBoolFn diagStarted = nullptr, diagPassed = nullptr, diagUseable = nullptr;

static DiagObject *DiagReadHook(DiagString *text, DiagObject *logger, const void *m) {
    DiagLogMap(DiagText(text));
    return diagRead(text, logger, m);
}
static void DiagSuccess(DiagObject *self, DiagObject *response, const void *m) {
    LOG_INFO("diag: HandleDownloadRequestSuccess");
    diagSuccess(self, response, m);
}
static void DiagOffline(DiagObject *self, DiagObject *args, const void *m) {
    LOG_INFO("diag: HandleOffline");
    diagOffline(self, args, m);
}
static void DiagRecord(DiagObject *self, const void *m) {
    LOG_INFO("diag: RecordCurrentVersions");
    diagRecord(self, m);
}
static void DiagClear(DiagObject *self, const void *m) {
    LOG_INFO("diag: ClearAllTrackedVersions");
    diagClear(self, m);
}
static DiagString *DiagLastKnown(DiagObject *self, const void *m) {
    DiagString *result = diagLastKnown(self, m);
    LOG_INFO("diag: GetLastKnownAssetVersion -> '%s'", DiagText(result).c_str());
    return result;
}
static bool DiagStarted(DiagObject *self, const void *m) {
    const bool result = diagStarted(self, m);
    LOG_INFO("diag: HasStartedDownloadAllBundlesMark -> %d", result);
    return result;
}
static bool DiagPassed(DiagObject *self, const void *m) {
    const bool result = diagPassed(self, m);
    LOG_INFO("diag: HasPassedDownloadAllBundlesMark -> %d", result);
    return result;
}
static bool DiagUseable(DiagObject *self, const void *m) {
    const bool result = diagUseable(self, m);
    LOG_INFO("diag: HasUseableBundles -> %d", result);
    return result;
}

static void InstallDiagnostics() {
    const auto reader = BNM::Class("PvZCards.Game", "AssetVersionMapReader");
    const auto command = BNM::Class("PvZCards.Game", "LoadAssetBundleVersionMapCommand");
    const auto tracker = BNM::Class("PvZCards.Game", "AssetBundleVersionTracker");
    BNM::BasicHook(reader.GetMethod("Read", 2), DiagReadHook, diagRead);
    BNM::BasicHook(command.GetMethod("HandleDownloadRequestSuccess", 1), DiagSuccess, diagSuccess);
    BNM::BasicHook(command.GetMethod("HandleOffline", 1), DiagOffline, diagOffline);
    BNM::BasicHook(tracker.GetMethod("RecordCurrentVersions", 0), DiagRecord, diagRecord);
    BNM::BasicHook(tracker.GetMethod("ClearAllTrackedVersions", 0), DiagClear, diagClear);
    BNM::BasicHook(tracker.GetMethod("GetLastKnownAssetVersion", 0), DiagLastKnown, diagLastKnown);
    BNM::BasicHook(tracker.GetMethod("HasStartedDownloadAllBundlesMark", 0), DiagStarted, diagStarted);
    BNM::BasicHook(tracker.GetMethod("HasPassedDownloadAllBundlesMark", 0), DiagPassed, diagPassed);
    BNM::BasicHook(tracker.GetMethod("HasUseableBundles", 0), DiagUseable, diagUseable);
    LOG_INFO("diag: hooks read=%d success=%d offline=%d record=%d clear=%d last=%d started=%d passed=%d useable=%d",
             !!diagRead, !!diagSuccess, !!diagOffline, !!diagRecord, !!diagClear, !!diagLastKnown,
             !!diagStarted, !!diagPassed, !!diagUseable);
}
```

Then make `InstallDiagnostics();` the first statement of `OnIl2CppLoaded()`.

- [ ] **Step 5: Build, inject and install the diagnostic build**

```powershell
.\work\scripts\build_mod.ps1; .\work\scripts\inject_mod.ps1; .\work\scripts\install.ps1
```

Expected: `Install OK.`

- [ ] **Step 6: Record two launches (the failing first run and the stuck relaunch)**

```powershell
. .\work\scripts\device.ps1
Start-SprigGame
Watch-Logcat -LogPath .\work\device-logs\task1-run1.log -TimeoutSeconds 600 -Classify { $null }
Start-SprigGame
Watch-Logcat -LogPath .\work\device-logs\task1-run2.log -TimeoutSeconds 300 -Classify { $null }
```

If the game shows a download prompt during run 1, accept it on the device. Each call returns `Timeout` when its window ends, which is expected here.

If the game crashes right after the `diag: hooks` line, before any map text is logged, an inline hook landed on a method too short to patch. Delete the four tracker-getter hooks (`GetLastKnownAssetVersion`, `HasStartedDownloadAllBundlesMark`, `HasPassedDownloadAllBundlesMark` and `HasUseableBundles`) from `InstallDiagnostics`, then repeat Steps 5 and 6. Record in Step 11 that the tracker value could not be observed.

- [ ] **Step 7: Extract the live map and read the evidence**

```powershell
. .\work\scripts\device.ps1
$map = ConvertFrom-ContentMapLog -LogPath .\work\device-logs\task1-run1.log
[System.IO.File]::WriteAllText("$PWD\work\content-map.txt", $map, [System.Text.UTF8Encoding]::new($false))
Select-String -Path .\work\device-logs\task1-run*.log -Pattern 'diag:|Sprig  |Next screen|\[MAPPING\]|Fatal signal|FATAL EXCEPTION'
```

Record these from the output:
1. The map format (line-based or JSON) and the exact text of the `1.67.8` association, including its `ServerVersion`.
2. The order of `diag:` lines relative to `Latest content association selected`. Note whether `GetLastKnownAssetVersion` runs before `HandleDownloadRequestSuccess` on the online path, and its value in run 2.
3. The `Next screen <Name>` line logged when the error dialog appeared, plus any crash lines.

- [ ] **Step 8: Apply the decision rules**

- If `1.67.8` is absent from the live map, stop and report to the owner. The spec's record source changes ("the most recent cached copy on the test device"), and case D becomes mandatory.
- If `GetLastKnownAssetVersion` runs before `HandleDownloadRequestSuccess` on the online path in run 2, stop. Update the spec's Hooks section (hook 3) and Task 4 Step 3 to hook the earliest method in the run 2 sequence whose `this` is the `LoadAssetBundleVersionMapCommand`, then continue.
- Otherwise continue unchanged.

- [ ] **Step 9: Revert the diagnostic patch**

```powershell
git checkout -- mod/src/main.cpp
git status --short mod/src/main.cpp
```

Expected: no output from `git status` (the file is clean).

- [ ] **Step 10: Write the content record**

Create `mod/content/association.txt` as the smallest document in the captured format that contains only the 1.67.8 association. Keep any header or wrapper the format needs, for example a JSON array with one element or the header line plus the 1.67.8 line. Write it byte-for-byte from `work\content-map.txt`, without retyping.

Create `mod/content/target.ini`, replacing `<pattern>` with a regex built from the Step 7 failure marker (for example `Next screen GenericErrorMsgBox`). `SuccessMarker` is added in Task 4.

```ini
# Verified content record. See docs/superpowers/specs/2026-10-13-verified-content-selection-design.md.
[Executable]
AppVersion=1.65.27

[Content]
# Newest asset version verified to run on the executable above.
AssetVersion=1.67.8

[Verification]
FailurePattern=<pattern>
```

- [ ] **Step 11: Record the evidence in the spec**

Under "Evidence to capture before implementation" in the spec, add a paragraph headed with the date of Step 6. Cover the map format, the 1.67.8 `ServerVersion`, the observed call order (paste the `diag:` sequence from run 2), the tracker values on the stuck path, and the failure marker.

- [ ] **Step 12: Commit**

```bash
git add .gitignore .gitattributes work/scripts/device.ps1 mod/content/target.ini mod/content/association.txt docs/superpowers/specs/2026-10-13-verified-content-selection-design.md
git commit -m "Capture verified content record for 1.65.27"
```

---

### Task 2: Content selection policy (host-tested)

**Files:**
- Create: `mod/include/content/policy.h`, `mod/src/content/policy.cpp`, `work/scripts/test_content.ps1`
- Modify: `work/scripts/test_all.ps1`, `.github/workflows/windows-tests.yml`

**Interfaces:**
- Produces (namespace `Content`, header `<content/policy.h>`):
  - `struct Version { std::array<std::int32_t, 4> parts{}; std::size_t count = 0; };`
  - `std::optional<Version> ParseVersion(std::string_view text);`
  - `int CompareVersions(const Version &left, const Version &right);` returns -1, 0 or 1
  - `bool IsVersion(std::string_view value, std::string_view expected);`
  - `std::optional<std::size_t> SelectAssociation(std::span<const std::string_view> assetVersions, std::string_view target);`
  - `std::optional<std::size_t> NewestUnverified(std::span<const std::string_view> assetVersions, std::string_view target);`
  - `bool NeedsTrackedVersionReset(std::string_view lastKnown, std::string_view target);`

- [ ] **Step 1: Write the header**

`mod/include/content/policy.h`:

```cpp
#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <optional>
#include <span>
#include <string_view>

namespace Content {
// One to four dotted int32 components; missing components compare as zero.
struct Version {
    std::array<std::int32_t, 4> parts{};
    std::size_t count = 0;
};

std::optional<Version> ParseVersion(std::string_view text);
int CompareVersions(const Version &left, const Version &right);
bool IsVersion(std::string_view value, std::string_view expected);

// The exact target, else the newest version below it. Equal versions resolve to
// the first in map order; malformed versions are never chosen.
std::optional<std::size_t> SelectAssociation(std::span<const std::string_view> assetVersions,
                                             std::string_view target);

// The newest version above the target, reported so unverified content is visible.
std::optional<std::size_t> NewestUnverified(std::span<const std::string_view> assetVersions,
                                            std::string_view target);

// True when the game tracks a non-empty content version other than the target.
bool NeedsTrackedVersionReset(std::string_view lastKnown, std::string_view target);
}
```

- [ ] **Step 2: Write the failing host test**

`work/scripts/test_content.ps1`:

```powershell
#requires -Version 5.1
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\native.ps1"
$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-content-$([guid]::NewGuid())"

function Write-Fixture {
    param([string]$Relative, [string]$Text)
    $path = Join-Path $sandbox $Relative
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    [System.IO.File]::WriteAllText($path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Assert-Content {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

try {
    # ---- Policy: compile the pure selection rules on the host and run their checks.
    New-Item -ItemType Directory -Force "$sandbox\policy\src" | Out-Null
    Copy-Item "$ProjectRoot\mod\include" "$sandbox\policy\include" -Recurse
    Copy-Item "$ProjectRoot\mod\src\content\policy.cpp" "$sandbox\policy\src\policy.cpp"
    Write-Fixture 'policy/CMakeLists.txt' @'
cmake_minimum_required(VERSION 3.22)
project(ContentPolicyCheck CXX)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
add_executable(policy_check check.cpp src/policy.cpp)
target_include_directories(policy_check PRIVATE include)
'@
    Write-Fixture 'policy/check.cpp' @'
#include <content/policy.h>

#include <cstdio>
#include <initializer_list>
#include <optional>
#include <string_view>
#include <vector>

static int failures = 0;
#define CHECK(condition) \
    do { if (!(condition)) { std::fprintf(stderr, "FAILED %s:%d: %s\n", __FILE__, __LINE__, #condition); ++failures; } } while (0)

using Versions = std::initializer_list<std::string_view>;

static std::optional<std::size_t> Select(Versions versions, std::string_view target) {
    const std::vector<std::string_view> list(versions);
    return Content::SelectAssociation(list, target);
}

static std::optional<std::size_t> Newer(Versions versions, std::string_view target) {
    const std::vector<std::string_view> list(versions);
    return Content::NewestUnverified(list, target);
}

static void CheckRejects(std::string_view text) {
    if (Content::ParseVersion(text)) {
        std::fprintf(stderr, "FAILED: accepted '%.*s'\n", static_cast<int>(text.size()), text.data());
        ++failures;
    }
}

static int Compare(std::string_view left, std::string_view right) {
    return Content::CompareVersions(*Content::ParseVersion(left), *Content::ParseVersion(right));
}

int main() {
    // Parsing: one to four decimal int32 components; outer whitespace is ignored.
    CHECK(Content::ParseVersion("1.67.8"));
    CHECK(Content::ParseVersion("10.0.0"));
    CHECK(Content::ParseVersion("1.67"));
    CHECK(Content::ParseVersion(" 1.67.8\r\n"));
    CHECK(Content::ParseVersion("2147483647.2147483647.2147483647.2147483647"));
    for (const std::string_view bad : {"", " ", "1..2", "1.2.x", "+1.2", "-1.2", "1.", ".1", "1 .2",
                                       "2147483648.0", "1.67.99999999999", "1.2.3.4.5", "1.2.3.4."})
        CheckRejects(bad);

    // Comparison is numeric per component.
    CHECK(Compare("1.67.10", "1.67.8") > 0);
    CHECK(Compare("1.68.0", "1.67.99") > 0);
    CHECK(Compare("1.66.7", "1.67.8") < 0);
    CHECK(Compare("1.67", "1.67.0") == 0);
    CHECK(Compare("1.67.8.0", "1.67.8") == 0);

    // Review focus: equal spellings are the same version.
    CHECK(Content::IsVersion(" 1.67.8.0", "1.67.8"));
    CHECK(!Content::IsVersion("1.67.80", "1.67.8"));
    CHECK(!Content::IsVersion("garbage", "1.67.8"));

    // Selection: exact target, else newest below it, else nothing.
    CHECK(Select({"1.66.7", "1.67.8", "1.68.10"}, "1.67.8") == 1u);
    CHECK(Select({"1.68.10", "1.66.7", "1.67.8"}, "1.67.8") == 2u);
    CHECK(Select({"1.66.7", "1.68.10"}, "1.67.8") == 0u);
    CHECK(Select({"1.65.27", "1.68.10", "1.66.7"}, "1.67.8") == 2u);
    CHECK(!Select({"1.68.10"}, "1.67.8"));
    CHECK(!Select({}, "1.67.8"));
    CHECK(Select({"garbage", "1.67.x", "1.67.8"}, "1.67.8") == 2u);
    CHECK(!Select({"1.67.8"}, "not-a-version"));
    // Review focus: duplicates resolve to the first in map order; equal spellings are the target.
    CHECK(Select({"1.68.10", "1.67.8", "1.67.8"}, "1.67.8") == 1u);
    CHECK(Select({"1.66.7", "1.66.7", "1.68.10"}, "1.67.8") == 0u);
    CHECK(Select({"1.66.7", "1.67.8.0"}, "1.67.8") == 1u);

    // Reporting: newest version above the target.
    CHECK(Newer({"1.66.7", "1.67.8", "1.68.10", "1.68.2"}, "1.67.8") == 2u);
    CHECK(!Newer({"1.66.7", "1.67.8"}, "1.67.8"));

    // Reset only when the game tracks a different, non-empty version.
    CHECK(!Content::NeedsTrackedVersionReset("", "1.67.8"));
    CHECK(!Content::NeedsTrackedVersionReset("  ", "1.67.8"));
    CHECK(!Content::NeedsTrackedVersionReset("1.67.8", "1.67.8"));
    CHECK(!Content::NeedsTrackedVersionReset("1.67.8.0", "1.67.8"));
    CHECK(Content::NeedsTrackedVersionReset("1.68.10", "1.67.8"));
    CHECK(Content::NeedsTrackedVersionReset("1.66.7", "1.67.8"));
    CHECK(Content::NeedsTrackedVersionReset("garbage", "1.67.8"));
    CHECK(!Content::NeedsTrackedVersionReset("1.68.10", "not-a-version"));

    if (failures == 0) std::puts("Content policy checks passed.");
    return failures == 0 ? 0 : 1;
}
'@
    Invoke-Native cmake -S "$sandbox\policy" -B "$sandbox\policy-build" -G 'Visual Studio 17 2022' -A x64
    Invoke-Native cmake --build "$sandbox\policy-build" --config Debug
    Invoke-Native "$sandbox\policy-build\Debug\policy_check.exe"

    Write-Host 'All content checks passed.'
} finally {
    # The target is the literal, uniquely named temp directory created above.
    if (Test-Path $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}
```

- [ ] **Step 3: Run it to verify it fails**

Run: `.\work\scripts\test_content.ps1`
Expected: FAIL. `Copy-Item` cannot find `mod\src\content\policy.cpp`.

- [ ] **Step 4: Implement the policy**

`mod/src/content/policy.cpp`:

```cpp
#include <content/policy.h>

#include <charconv>

namespace {
std::string_view Trim(std::string_view value) {
    const auto start = value.find_first_not_of(" \t\r\n");
    if (start == value.npos) return {};
    return value.substr(start, value.find_last_not_of(" \t\r\n") - start + 1);
}

// Index of the newest parseable version accepted by `accept`; ties keep the first in map order.
template <typename Accept>
std::optional<std::size_t> Newest(std::span<const std::string_view> versions, Accept accept) {
    std::optional<std::size_t> best;
    Content::Version bestVersion;
    for (std::size_t i = 0; i < versions.size(); ++i) {
        const auto version = Content::ParseVersion(versions[i]);
        if (!version || !accept(*version)) continue;
        if (!best || Content::CompareVersions(*version, bestVersion) > 0) {
            best = i;
            bestVersion = *version;
        }
    }
    return best;
}
}

namespace Content {
std::optional<Version> ParseVersion(std::string_view text) {
    text = Trim(text);
    if (text.empty()) return std::nullopt;
    Version version;
    while (version.count < version.parts.size()) {
        const auto dot = text.find('.');
        const auto part = text.substr(0, dot);
        if (part.empty() || part.find_first_not_of("0123456789") != part.npos) return std::nullopt;
        auto &value = version.parts[version.count];
        const auto parsed = std::from_chars(part.data(), part.data() + part.size(), value);
        if (parsed.ec != std::errc{} || parsed.ptr != part.data() + part.size()) return std::nullopt;
        ++version.count;
        if (dot == text.npos) return version;
        text.remove_prefix(dot + 1);
    }
    return std::nullopt;
}

int CompareVersions(const Version &left, const Version &right) {
    for (std::size_t i = 0; i < left.parts.size(); ++i)
        if (left.parts[i] != right.parts[i]) return left.parts[i] < right.parts[i] ? -1 : 1;
    return 0;
}

bool IsVersion(std::string_view value, std::string_view expected) {
    const auto left = ParseVersion(value);
    const auto right = ParseVersion(expected);
    return left && right && CompareVersions(*left, *right) == 0;
}

std::optional<std::size_t> SelectAssociation(std::span<const std::string_view> assetVersions,
                                             std::string_view target) {
    const auto wanted = ParseVersion(target);
    if (!wanted) return std::nullopt;
    for (std::size_t i = 0; i < assetVersions.size(); ++i)
        if (IsVersion(assetVersions[i], target)) return i;
    return Newest(assetVersions, [&](const Version &version) { return CompareVersions(version, *wanted) < 0; });
}

std::optional<std::size_t> NewestUnverified(std::span<const std::string_view> assetVersions,
                                            std::string_view target) {
    const auto wanted = ParseVersion(target);
    if (!wanted) return std::nullopt;
    return Newest(assetVersions, [&](const Version &version) { return CompareVersions(version, *wanted) > 0; });
}

bool NeedsTrackedVersionReset(std::string_view lastKnown, std::string_view target) {
    if (Trim(lastKnown).empty() || !ParseVersion(target)) return false;
    return !IsVersion(lastKnown, target);
}
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `.\work\scripts\test_content.ps1`
Expected: `Content policy checks passed.` then `All content checks passed.`

- [ ] **Step 6: Run it from the suite and CI**

Append to `work/scripts/test_all.ps1`:

```powershell
& "$PSScriptRoot\test_content.ps1"
```

In `.github/workflows/windows-tests.yml`, add to the `regression-tests` job after the `PowerShell 7` step:

```yaml
      - name: Content checks
        shell: powershell
        run: .\work\scripts\test_content.ps1
```

Run: `.\work\scripts\test_pipeline.ps1`
Expected: no parse-error failures for the new script.

- [ ] **Step 7: Commit**

```bash
git add mod/include/content/policy.h mod/src/content/policy.cpp work/scripts/test_content.ps1 work/scripts/test_all.ps1 .github/workflows/windows-tests.yml
git commit -m "Add verified content selection policy"
```

---

### Task 3: Embed and validate the content record

**Files:**
- Create: `mod/cmake/ContentRecord.cmake`, `mod/cmake/Content.cmake`, `mod/include/content/record.h`
- Modify: `mod/CMakeLists.txt`, `work/scripts/build_mod.ps1`, `work/scripts/test_content.ps1`, `work/scripts/test_pipeline.ps1`

**Interfaces:**
- Consumes: `mod/content/target.ini`, `mod/content/association.txt` (Task 1).
- Produces: `sprig_load_content_record(<dir>)`, which sets `SPRIG_CONTENT_APP_VERSION`, `SPRIG_CONTENT_ASSET_VERSION` and `SPRIG_CONTENT_ASSOCIATION_HEX` in the caller's scope. Also `struct Content::Record { std::string_view appVersion, assetVersion, association; }` and `const Content::Record &Content::GetRecord();`. Build outputs: `work/build-out/cmake/content-record.cpp` and `work/build-out/cmake/content-executable.txt`. CMake cache options: `SPRIG_CONTENT_DIR`, `SPRIG_TEST_DROP_TARGET` and `SPRIG_LOG_CONTENT_MAP`, which become compile definitions of the same names when ON. `build_mod.ps1` gains `-ContentDir`, `-DropTarget` and `-LogContentMap`.

- [ ] **Step 1: Write the failing record checks**

In `work/scripts/test_content.ps1`, insert before `Write-Host 'All content checks passed.'`:

```powershell
    # ---- Record: ContentRecord.cmake must accept the real record and reject broken ones.
    Write-Fixture 'record/check_record.cmake' @'
include("${SPRIG_MOD_DIR}/cmake/ContentRecord.cmake")
sprig_load_content_record("${SPRIG_CONTENT_DIR}")
message(STATUS "app=${SPRIG_CONTENT_APP_VERSION}")
message(STATUS "asset=${SPRIG_CONTENT_ASSET_VERSION}")
message(STATUS "hex=${SPRIG_CONTENT_ASSOCIATION_HEX}")
'@
    function Invoke-RecordCheck {
        param([string]$ContentDir)
        $oldPreference = $ErrorActionPreference
        try {
            # cmake -P reports validation failures on stderr with a nonzero exit code.
            $ErrorActionPreference = 'Continue'
            $output = @(& cmake "-DSPRIG_MOD_DIR=$ProjectRoot\mod" "-DSPRIG_CONTENT_DIR=$ContentDir" -P "$sandbox\record\check_record.cmake" 2>&1 | ForEach-Object { "$_" })
            $code = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $oldPreference
        }
        return [pscustomobject]@{ Code = $code; Text = ($output -join "`n") }
    }
    function Write-Record {
        param([string]$Name, $Ini, [byte[]]$Association)
        $dir = Join-Path $sandbox "record\$Name"
        New-Item -ItemType Directory -Force $dir | Out-Null
        if ($null -ne $Ini) { [System.IO.File]::WriteAllText((Join-Path $dir 'target.ini'), $Ini, [System.Text.UTF8Encoding]::new($false)) }
        if ($null -ne $Association) { [System.IO.File]::WriteAllBytes((Join-Path $dir 'association.txt'), $Association) }
        return $dir
    }
    function Assert-RecordRejected {
        param([string]$Dir, [string]$Pattern)
        $result = Invoke-RecordCheck $Dir
        Assert-Content ($result.Code -ne 0 -and $result.Text -match $Pattern) "Record in $Dir should fail with '$Pattern' but got exit $($result.Code): $($result.Text)"
    }

    $real = Invoke-RecordCheck "$ProjectRoot\mod\content"
    Assert-Content ($real.Code -eq 0) "The committed content record must validate: $($real.Text)"

    $ini = "[Executable]`nAppVersion=1.65.27`n[Content]`nAssetVersion=1.67.8`n[Verification]`nSuccessMarker=Next screen Home`n"
    $utf8 = [System.Text.Encoding]::UTF8
    $bom = [byte[]](0xEF, 0xBB, 0xBF)
    # Review focus: an editor-added BOM is dropped and CRLF is kept byte-for-byte.
    $valid = Invoke-RecordCheck (Write-Record 'valid' $ini ([byte[]]($bom + $utf8.GetBytes("header`r`n1.67.8,abc`r`n"))))
    Assert-Content ($valid.Code -eq 0) "A valid record must load: $($valid.Text)"
    Assert-Content ($valid.Text -match 'app=1\.65\.27' -and $valid.Text -match 'asset=1\.67\.8') 'Record versions were not read.'
    Assert-Content ($valid.Text -match 'hex=6865616465720d0a') 'The BOM must be dropped and CRLF preserved.'

    $association = $utf8.GetBytes("header`n1.67.8,abc`n")
    Assert-RecordRejected (Write-Record 'no-association' $ini $null) 'missing'
    Assert-RecordRejected (Write-Record 'no-key' "[Executable]`nAppVersion=1.65.27`n[Content]`n" $association) 'exactly one \[Content\] AssetVersion.+found 0'
    Assert-RecordRejected (Write-Record 'duplicate' ($ini + "[Content]`nAssetVersion=1.67.8`n") $association) 'found 2'
    Assert-RecordRejected (Write-Record 'bad-version' ($ini.Replace('1.67.8', '1.67.x')) ($utf8.GetBytes('1.67.x'))) 'not a dotted version'
    # Review focus: a component the runtime cannot parse must fail at configure time.
    Assert-RecordRejected (Write-Record 'overflow' ($ini.Replace('1.67.8', '1.67.2147483648')) ($utf8.GetBytes('1.67.2147483648'))) 'not a dotted version'
    Assert-RecordRejected (Write-Record 'unrelated' $ini ($utf8.GetBytes("header`n1.66.7,abc`n"))) 'does not mention 1\.67\.8'
    Assert-RecordRejected (Write-Record 'empty' $ini $bom) 'is empty'
    Write-Host 'Content record checks passed.'
```

- [ ] **Step 2: Run it to verify it fails**

Run: `.\work\scripts\test_content.ps1`
Expected: FAIL with `The committed content record must validate`, because `ContentRecord.cmake` does not exist yet.

- [ ] **Step 3: Implement `mod/cmake/ContentRecord.cmake`**

```cmake
# Loads and validates the verified content record. Script-mode safe: defines no targets.

function(sprig_content_read_key ini section key out)
    file(STRINGS "${ini}" lines ENCODING UTF-8)
    set(current "")
    set(found "")
    set(count 0)
    foreach(line IN LISTS lines)
        string(STRIP "${line}" line)
        if(line MATCHES "^\\[([^]]+)\\]$")
            string(STRIP "${CMAKE_MATCH_1}" current)
        elseif("${current}" STREQUAL "${section}" AND line MATCHES "^([^=#;]+)=(.*)$")
            string(STRIP "${CMAKE_MATCH_1}" name)
            if("${name}" STREQUAL "${key}")
                string(STRIP "${CMAKE_MATCH_2}" found)
                math(EXPR count "${count} + 1")
            endif()
        endif()
    endforeach()
    if(NOT count EQUAL 1)
        message(FATAL_ERROR "Content: expected exactly one [${section}] ${key} in ${ini}, found ${count}")
    endif()
    set(${out} "${found}" PARENT_SCOPE)
endfunction()

# Mirrors Content::ParseVersion: one to four int32 components.
function(sprig_content_check_version value source)
    if(NOT value MATCHES "^[0-9]+(\\.[0-9]+)?(\\.[0-9]+)?(\\.[0-9]+)?$")
        message(FATAL_ERROR "Content: '${value}' in ${source} is not a dotted version")
    endif()
    string(REPLACE "." ";" parts "${value}")
    foreach(part IN LISTS parts)
        string(LENGTH "${part}" digits)
        if(digits GREATER 10 OR part GREATER 2147483647)
            message(FATAL_ERROR "Content: '${value}' in ${source} is not a dotted version (component above 2147483647)")
        endif()
    endforeach()
endfunction()

function(sprig_load_content_record dir)
    set(ini "${dir}/target.ini")
    set(association "${dir}/association.txt")
    foreach(required IN ITEMS "${ini}" "${association}")
        if(NOT EXISTS "${required}")
            message(FATAL_ERROR "Content: missing ${required}")
        endif()
    endforeach()
    sprig_content_read_key("${ini}" Executable AppVersion app_version)
    sprig_content_read_key("${ini}" Content AssetVersion asset_version)
    sprig_content_check_version("${app_version}" "${ini}")
    sprig_content_check_version("${asset_version}" "${ini}")

    # Hex keeps the captured map text byte-exact; only an editor's UTF-8 BOM is dropped.
    file(READ "${association}" association_hex HEX)
    string(REGEX REPLACE "^efbbbf" "" association_hex "${association_hex}")
    if(association_hex STREQUAL "")
        message(FATAL_ERROR "Content: ${association} is empty")
    endif()
    file(READ "${association}" association_text)
    string(FIND "${association_text}" "${asset_version}" at)
    if(at EQUAL -1)
        message(FATAL_ERROR "Content: ${association} does not mention ${asset_version}")
    endif()

    set(SPRIG_CONTENT_APP_VERSION "${app_version}" PARENT_SCOPE)
    set(SPRIG_CONTENT_ASSET_VERSION "${asset_version}" PARENT_SCOPE)
    set(SPRIG_CONTENT_ASSOCIATION_HEX "${association_hex}" PARENT_SCOPE)
endfunction()
```

- [ ] **Step 4: Run the record checks to verify they pass**

Run: `.\work\scripts\test_content.ps1`
Expected: `Content policy checks passed.`, `Content record checks passed.`, `All content checks passed.`

- [ ] **Step 5: Embed the record in the Android build**

`mod/include/content/record.h`:

```cpp
#pragma once

#include <string_view>

namespace Content {
// Generated from mod/content by cmake/Content.cmake.
struct Record {
    std::string_view appVersion;
    std::string_view assetVersion;
    std::string_view association;
};

const Record &GetRecord();
}
```

`mod/cmake/Content.cmake`:

```cmake
# Embeds the verified content record (mod/content) into libsprig.so.
include("${CMAKE_CURRENT_LIST_DIR}/ContentRecord.cmake")
set(SPRIG_CONTENT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/content" CACHE PATH "Verified content record directory")
option(SPRIG_TEST_DROP_TARGET "Debug: hide the verified target from the live map so it is restored" OFF)
option(SPRIG_LOG_CONTENT_MAP "Debug: log the raw content map for capture_content_map.ps1" OFF)

sprig_load_content_record("${SPRIG_CONTENT_DIR}")
set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS
    "${SPRIG_CONTENT_DIR}/target.ini" "${SPRIG_CONTENT_DIR}/association.txt")

string(REGEX REPLACE "(..)" "\\\\x\\1" content_association_literal "${SPRIG_CONTENT_ASSOCIATION_HEX}")
file(CONFIGURE OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/content-record.cpp" CONTENT "#include <content/record.h>
namespace Content {
const Record &GetRecord() {
    static const Record record{\"${SPRIG_CONTENT_APP_VERSION}\", \"${SPRIG_CONTENT_ASSET_VERSION}\",
                               \"${content_association_literal}\"};
    return record;
}
}
" @ONLY)
# inject_mod.ps1 refuses an input APK this record was not verified against.
file(CONFIGURE OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/content-executable.txt"
    CONTENT "${SPRIG_CONTENT_APP_VERSION}\n" @ONLY)
target_sources(sprig PRIVATE "${CMAKE_CURRENT_BINARY_DIR}/content-record.cpp")

foreach(flag SPRIG_TEST_DROP_TARGET SPRIG_LOG_CONTENT_MAP)
    if(${flag})
        target_compile_definitions(sprig PRIVATE ${flag}=1)
        message(WARNING "Content: ${flag} is a debug-only build option")
    endif()
endforeach()
message(STATUS "Content: executable ${SPRIG_CONTENT_APP_VERSION}, target ${SPRIG_CONTENT_ASSET_VERSION}")
```

In `mod/CMakeLists.txt`, add `include(cmake/Content.cmake)` on the line after `include(cmake/Bloom.cmake)`.

- [ ] **Step 6: Pass content options explicitly from `build_mod.ps1`**

In `work/scripts/build_mod.ps1`, insert after the closing `#>` of the help block (line 8):

```powershell
param(
    # Directory with target.ini and association.txt. Defaults to mod\content.
    [string]$ContentDir,
    # Debug: acceptance-test build that hides the target from the live map.
    [switch]$DropTarget,
    # Debug: log the raw content map for capture_content_map.ps1.
    [switch]$LogContentMap
)
```

After `$CmakeDir    = Join-Path $BuildOut 'cmake'` add:

```powershell
# Pass every content option on each configure so a debug build never sticks in the CMake cache.
if (-not $ContentDir) { $ContentDir = Join-Path $ModDir 'content' }
$ContentDir = (Resolve-Path $ContentDir).Path -replace '\\', '/'
$DropTargetValue = if ($DropTarget) { 'ON' } else { 'OFF' }
$LogContentMapValue = if ($LogContentMap) { 'ON' } else { 'OFF' }
```

Replace the configure call's last line `'-DCMAKE_BUILD_TYPE=Release'` with:

```powershell
    '-DCMAKE_BUILD_TYPE=Release' `
    "-DSPRIG_CONTENT_DIR=$ContentDir" `
    "-DSPRIG_TEST_DROP_TARGET=$DropTargetValue" `
    "-DSPRIG_LOG_CONTENT_MAP=$LogContentMapValue"
```

In `work/scripts/test_pipeline.ps1`, after the `$inject` assertions, add:

```powershell
$buildMod = Read-Script 'build_mod.ps1'
Assert-True ($buildMod -match 'SPRIG_CONTENT_DIR=') 'build_mod.ps1 must pass SPRIG_CONTENT_DIR on every configure.'
Assert-True ($buildMod -match 'SPRIG_TEST_DROP_TARGET=' -and $buildMod -match 'SPRIG_LOG_CONTENT_MAP=') 'build_mod.ps1 must pass every content debug option on every configure.'
```

- [ ] **Step 7: Verify the Android build and the tests**

Run: `.\work\scripts\build_mod.ps1`
Expected: configure prints `Content: executable 1.65.27, target 1.67.8`, the build ends with `Built ...libsprig.so`, and `work\build-out\cmake\content-executable.txt` contains `1.65.27`.

Run: `.\work\scripts\test_pipeline.ps1; .\work\scripts\test_content.ps1`
Expected: both pass.

- [ ] **Step 8: Commit**

```bash
git add mod/cmake/ContentRecord.cmake mod/cmake/Content.cmake mod/include/content/record.h mod/CMakeLists.txt work/scripts/build_mod.ps1 work/scripts/test_content.ps1 work/scripts/test_pipeline.ps1
git commit -m "Embed and validate the verified content record"
```

---

### Task 4: Content hooks

**Files:**
- Create: `mod/include/content/hooks.h`, `mod/src/content/hooks.cpp`
- Modify: `mod/src/main.cpp`, `mod/include/log.h`, `mod/CMakeLists.txt`, `work/scripts/test_pipeline.ps1`, `mod/content/target.ini`

**Interfaces:**
- Consumes: `Content::SelectAssociation`, `Content::NewestUnverified`, `Content::IsVersion` and `Content::NeedsTrackedVersionReset` (Task 2). `Content::GetRecord()` (Task 3). The `SPRIG_TEST_DROP_TARGET` and `SPRIG_LOG_CONTENT_MAP` compile definitions (Task 3).
- Produces: `void Content::InstallHooks();` and these exact log lines. Later tasks match them literally:
  - `content: FindAssociation hook OK`, `content: Read hook OK`, `content: reset hook OK` (or `... unavailable`)
  - `Content map: <n> associations`
  - `Content target <v> missing from live map; restored from record`
  - `content: test build dropped <v> from the live map`
  - `Newer content <v> available (unverified)`
  - `Content selected: <v>`
  - `Content state reset: tracked <old>, target <v>`
  - `content: no verified association at or below <v> for app <app>` (fatal level)
  - `content: map text begin (<n> bytes)`, `content: map NNNN <payload>`, `content: map text end` (only with `SPRIG_LOG_CONTENT_MAP`)

- [ ] **Step 1: Write the failing static checks**

In `work/scripts/test_pipeline.ps1`, after the `$buildMod` assertions, add:

```powershell
$main = Get-Content -Raw (Join-Path $ProjectRoot 'mod\src\main.cpp')
Assert-True ($main -notmatch '2147483647') 'main.cpp must not select the newest unverified content.'
Assert-True ($main -match 'Content::InstallHooks\(\)') 'main.cpp must install the verified content hooks.'
```

Run: `.\work\scripts\test_pipeline.ps1`
Expected: FAIL with both messages.

- [ ] **Step 2: Add `LOG_FATAL` and the hooks header**

In `mod/include/log.h`, add after the `LOG_ERR` line:

```cpp
#define LOG_FATAL(...) __android_log_print(ANDROID_LOG_FATAL, LOG_TAG, __VA_ARGS__)
```

`mod/include/content/hooks.h`:

```cpp
#pragma once

namespace Content {
// Installs verified content selection. Call from the BNM loaded event, before managed code runs.
void InstallHooks();
}
```

- [ ] **Step 3: Implement `mod/src/content/hooks.cpp`**

```cpp
#include <content/hooks.h>

#include <string>
#include <string_view>
#include <vector>

#include <BNM/Class.hpp>
#include <BNM/Exceptions.hpp>
#include <BNM/Field.hpp>
#include <BNM/Method.hpp>
#include <BNM/MethodBase.hpp>
#include <BNM/Utils.hpp>

#include <content/policy.h>
#include <content/record.h>
#include "log.h"

namespace {
using Object = BNM::IL2CPP::Il2CppObject;
using String = BNM::Structures::Mono::String;
using ObjectList = BNM::Structures::Mono::List<Object *>;

using ReadFn = Object *(*)(String *, Object *, const void *);
using FindFn = Object *(*)(Object *, String *, const void *);
using HandleSuccessFn = void (*)(Object *, String *, const void *);

ReadFn originalRead = nullptr;
FindFn originalFind = nullptr;
HandleSuccessFn originalHandleSuccess = nullptr;

BNM::Field<ObjectList *> associationsField;
BNM::Field<String *> assetVersionField;
BNM::Method<void> addAssociation;
BNM::Method<Object *> getTracker;
BNM::Method<String *> getLastKnownAssetVersion;
BNM::Method<void> clearAllTrackedVersions;

std::string Text(String *value) { return value ? value->str() : std::string{}; }

std::string Target() { return std::string(Content::GetRecord().assetVersion); }

ObjectList *Associations(Object *map) { return map ? associationsField[map].Get() : nullptr; }

// IL2CPP strings are copied out so the policy only sees standard types.
std::vector<std::string> AssetVersions(ObjectList *list) {
    std::vector<std::string> versions;
    if (!list) return versions;
    for (int i = 0; i < list->GetSize(); ++i) {
        Object *association = list->GetData()[i];
        versions.push_back(association ? Text(assetVersionField[association].Get()) : std::string{});
    }
    return versions;
}

std::vector<std::string_view> Views(const std::vector<std::string> &values) {
    return {values.begin(), values.end()};
}

void LogException(const char *where, const BNM::Exception &exception) {
    LOG_ERR("content: %s threw %s: %s", where, exception.ClassName().c_str(), exception.Message().c_str());
}

#ifdef SPRIG_LOG_CONTENT_MAP
// capture_content_map.ps1 reassembles these chunks; escapes keep line breaks intact.
void LogMapText(const std::string &text) {
    std::string escaped;
    for (char c : text) {
        if (c == '\\') escaped += "\\\\";
        else if (c == '\n') escaped += "\\n";
        else if (c == '\r') escaped += "\\r";
        else escaped += c;
    }
    LOG_INFO("content: map text begin (%zu bytes)", text.size());
    for (std::size_t at = 0, part = 0; at < escaped.size(); at += 800, ++part)
        LOG_INFO("content: map %04zu %s", part, escaped.substr(at, 800).c_str());
    LOG_INFO("content: map text end");
}
#endif

#ifdef SPRIG_TEST_DROP_TARGET
// Acceptance case D: behave as if EA had removed the target from the live map.
void DropTarget(ObjectList *list) {
    const auto versions = AssetVersions(list);
    Object **items = list->GetData();
    int kept = 0;
    for (int i = 0; i < list->GetSize(); ++i)
        if (!Content::IsVersion(versions[i], Target())) items[kept++] = items[i];
    for (int i = kept; i < list->GetSize(); ++i) items[i] = nullptr;
    list->size = kept;
    ++list->version;
    LOG_WARN("content: test build dropped %s from the live map", Target().c_str());
}
#endif

void RestoreTarget(Object *map, Object *logger, const void *method) {
    ObjectList *list = Associations(map);
#ifdef SPRIG_TEST_DROP_TARGET
    if (list) DropTarget(list);
#endif
    const auto versions = AssetVersions(list);
    LOG_INFO("Content map: %zu associations", versions.size());
    for (const auto &version : versions)
        if (Content::IsVersion(version, Target())) return;

    // The game's own parser builds the association from the captured map text.
    Object *recordMap = originalRead(BNM::CreateMonoString(Content::GetRecord().association), logger, method);
    ObjectList *restored = Associations(recordMap);
    if (!restored || restored->GetSize() == 0) {
        LOG_ERR("content: record association did not parse");
        return;
    }
    for (int i = 0; i < restored->GetSize(); ++i) addAssociation[map](restored->GetData()[i]);
    LOG_WARN("Content target %s missing from live map; restored from record", Target().c_str());
}

Object *HookRead(String *text, Object *logger, const void *method) {
#ifdef SPRIG_LOG_CONTENT_MAP
    LogMapText(Text(text));
#endif
    Object *map = originalRead(text, logger, method);
    if (!map) return map;
    const auto exception = BNM::TryInvoke([&] { RestoreTarget(map, logger, method); });
    if (exception.IsValid()) LogException("AssetVersionMapReader.Read", exception);
    return map;
}

Object *SelectVerified(Object *map) {
    ObjectList *list = Associations(map);
    const auto versions = AssetVersions(list);
    const auto views = Views(versions);
    if (const auto newer = Content::NewestUnverified(views, Target()))
        LOG_INFO("Newer content %s available (unverified)", versions[*newer].c_str());
    const auto chosen = Content::SelectAssociation(views, Target());
    if (!chosen) return nullptr;
    LOG_INFO("Content selected: %s", versions[*chosen].c_str());
    return list->GetData()[*chosen];
}

Object *HookFind(Object *map, String *appVersion, const void *method) {
    Object *chosen = nullptr;
    const auto exception = BNM::TryInvoke([&] { chosen = SelectVerified(map); });
    if (exception.IsValid()) LogException("AssetVersionMap.FindAssociation", exception);
    if (chosen) return chosen;
    LOG_FATAL("content: no verified association at or below %s for app %s",
              Target().c_str(), Text(appVersion).c_str());
    return originalFind(map, appVersion, method);
}

void ResetTrackedVersions(Object *command) {
    Object *tracker = getTracker[command]();
    if (!tracker) {
        LOG_WARN("content: reset skipped (no AssetBundleVersionTracker)");
        return;
    }
    const auto lastKnown = Text(getLastKnownAssetVersion[tracker]());
    if (!Content::NeedsTrackedVersionReset(lastKnown, Target())) return;
    clearAllTrackedVersions[tracker]();
    LOG_INFO("Content state reset: tracked %s, target %s", lastKnown.c_str(), Target().c_str());
}

void HookHandleSuccess(Object *command, String *response, const void *method) {
    const auto exception = BNM::TryInvoke([&] { ResetTrackedVersions(command); });
    if (exception.IsValid()) LOG_WARN("content: reset skipped (%s)", exception.Message().c_str());
    originalHandleSuccess(command, response, method);
}
}

void Content::InstallHooks() {
    const auto map = BNM::Class("PvZCards.Game", "AssetVersionMap");
    const auto association = BNM::Class("PvZCards.Game", "AssetVersionAssociation");
    const auto reader = BNM::Class("PvZCards.Game", "AssetVersionMapReader");
    const auto command = BNM::Class("PvZCards.Game", "LoadAssetBundleVersionMapCommand");
    const auto tracker = BNM::Class("PvZCards.Game", "AssetBundleVersionTracker");

    associationsField = map.GetField("Associations");
    assetVersionField = association.GetField("AssetVersion");
    if (!associationsField.IsValid() || !assetVersionField.IsValid()) {
        LOG_ERR("content: FindAssociation hook unavailable (AssetVersionMap fields not found)");
        return;
    }
    BNM::BasicHook(map.GetMethod("FindAssociation", 1), HookFind, originalFind);
    if (!originalFind) {
        LOG_ERR("content: FindAssociation hook unavailable");
        return;
    }
    LOG_INFO("content: FindAssociation hook OK");

    // Restore and reset are independent safety layers; selection works without them.
    addAssociation = map.GetMethod("AddAssociation", 1);
    if (addAssociation.IsValid()) BNM::BasicHook(reader.GetMethod("Read", 2), HookRead, originalRead);
    if (originalRead) LOG_INFO("content: Read hook OK");
    else LOG_ERR("content: Read hook unavailable");

    getTracker = command.GetMethod("get_AssetBundleVersionTracker", 0);
    getLastKnownAssetVersion = tracker.GetMethod("GetLastKnownAssetVersion", 0);
    clearAllTrackedVersions = tracker.GetMethod("ClearAllTrackedVersions", 0);
    if (getTracker.IsValid() && getLastKnownAssetVersion.IsValid() && clearAllTrackedVersions.IsValid())
        BNM::BasicHook(command.GetMethod("HandleDownloadRequestSuccess", 1), HookHandleSuccess, originalHandleSuccess);
    if (originalHandleSuccess) LOG_INFO("content: reset hook OK");
    else LOG_ERR("content: reset hook unavailable");
}
```

If Task 1 Step 8 moved the reset to another method, use that method's name and signature in `HookHandleSuccess` and in the final `BasicHook` call instead.

- [ ] **Step 4: Wire it into `main.cpp` and CMake**

Replace `mod/src/main.cpp` with:

```cpp
#include <jni.h>

#include <BNM/Class.hpp>
#include <BNM/Loading.hpp>
#include <BNM/MethodBase.hpp>

#include "log.h"
#include <bloom/bloom.h>
#include <content/hooks.h>

using FailsMinimumAppVersion = bool (*)(void *, const void *);
static FailsMinimumAppVersion originalFailsMinimumAppVersion = nullptr;

static bool BlockUpdate(void *, const void *) { return false; }

static void InstallBuiltInHooks() {
    auto minimumAppVersion = BNM::Class("PvZCards.Application", "EnforceMinimumAppVersionCommand")
        .GetMethod("FailsMinimumAppVersion", 0);
    if (!minimumAppVersion.IsValid()) {
        LOG_ERR("EnforceMinimumAppVersionCommand.FailsMinimumAppVersion was not found");
        return;
    }

    BNM::BasicHook(minimumAppVersion, BlockUpdate, originalFailsMinimumAppVersion);
    if (!originalFailsMinimumAppVersion) {
        LOG_ERR("EnforceMinimumAppVersionCommand.FailsMinimumAppVersion hook failed");
        return;
    }

    LOG_INFO("Sprig loaded");
}

static void OnIl2CppLoaded() {
    InstallBuiltInHooks();
    Content::InstallHooks();
    Bloom::InitializePnP();
}

extern "C" JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM *vm, void * /*reserved*/) {
    JNIEnv *env = nullptr;
    if (vm->GetEnv(reinterpret_cast<void **>(&env), JNI_VERSION_1_6) != JNI_OK || env == nullptr) {
        LOG_ERR("JNI_OnLoad: GetEnv failed");
        return JNI_VERSION_1_6;
    }

    BNM::Loading::AllowLateInitHook();
    BNM::Loading::AddOnLoadedEvent(&OnIl2CppLoaded);

    if (!BNM::Loading::TryLoadByJNI(env)) {
        LOG_ERR("BNM TryLoadByJNI returned false (relying on late-init hook)");
    } else {
        LOG_INFO("BNM TryLoadByJNI ok; awaiting il2cpp_init");
    }

    return JNI_VERSION_1_6;
}
```

In `mod/CMakeLists.txt`, change the library sources to:

```cmake
add_library(sprig SHARED
    src/main.cpp
    src/bloom.cpp
    src/content/policy.cpp
    src/content/hooks.cpp
)
```

- [ ] **Step 5: Verify the static checks, host tests and Android build**

Run: `.\work\scripts\test_pipeline.ps1; .\work\scripts\test_content.ps1; .\work\scripts\build_mod.ps1`
Expected: all pass, and `Built ...libsprig.so`.

- [ ] **Step 6: Device smoke run and success marker capture**

```powershell
.\work\scripts\inject_mod.ps1; .\work\scripts\install.ps1
. .\work\scripts\device.ps1
Start-SprigGame
Watch-Logcat -LogPath .\work\device-logs\task4-smoke.log -TimeoutSeconds 1800 -Classify {
    param($text)
    if ($text -match $SprigCrashPattern) { 'Crash' }
}
```

Accept a download prompt on the device if one appears. When the home screen is visible on the device, press Ctrl+C and run:

```powershell
adb exec-out screencap -p > .\work\device-logs\task4-home.png
Select-String -Path .\work\device-logs\task4-smoke.log -Pattern 'Sprig  |Next screen'
```

Expected: `content: FindAssociation hook OK`, `content: Read hook OK`, `content: reset hook OK`, `Content map: ...`, `Newer content 1.68.10 available (unverified)` and `Content selected: 1.67.8`. This device was left on 1.68.10 by Task 1, so expect a `Content state reset: tracked ...` line as well if the tracker held a version. There must be no `Crash`. The last `Next screen <Name>` before the home screen in the screenshot is the success marker.

Add it to `mod/content/target.ini` under `[Verification]`:

```ini
SuccessMarker=Next screen <Name>
```

- [ ] **Step 7: Commit**

```bash
git add mod/include/content/hooks.h mod/src/content/hooks.cpp mod/src/main.cpp mod/include/log.h mod/CMakeLists.txt work/scripts/test_pipeline.ps1 mod/content/target.ini
git commit -m "Select verified content and repair unverified state"
```

---

### Task 5: Build guard for the executable

**Files:**
- Create: `work/scripts/content_guard.ps1`
- Modify: `work/scripts/inject_mod.ps1`, `work/scripts/test_content.ps1`, `work/scripts/test_pipeline.ps1`

**Interfaces:**
- Consumes: `work/build-out/cmake/content-executable.txt` (Task 3).
- Produces: `Assert-ContentExecutable -ApktoolConfig <path> -TargetStamp <path>`, which throws on any mismatch.

- [ ] **Step 1: Write the failing guard checks**

In `work/scripts/test_content.ps1`, insert before `Write-Host 'All content checks passed.'`:

```powershell
    # ---- Guard: inject_mod.ps1 must refuse an input APK the record was not verified against.
    . "$PSScriptRoot\content_guard.ps1"
    Write-Fixture 'guard/apktool.yml' "version: 3.0.2`nversionInfo:`n  versionCode: 293`n  versionName: 1.65.27`n"
    Write-Fixture 'guard/quoted.yml' "versionInfo:`n  versionName: '1.65.27'`n"
    Write-Fixture 'guard/other.yml' "versionInfo:`n  versionName: 1.66.7`n"
    Write-Fixture 'guard/stamp.txt' "1.65.27`n"
    Assert-ContentExecutable -ApktoolConfig "$sandbox\guard\apktool.yml" -TargetStamp "$sandbox\guard\stamp.txt"
    Assert-ContentExecutable -ApktoolConfig "$sandbox\guard\quoted.yml" -TargetStamp "$sandbox\guard\stamp.txt"
    # Review focus: swapping input\Game.apk for another version stops the build.
    $guardError = $null
    try { Assert-ContentExecutable -ApktoolConfig "$sandbox\guard\other.yml" -TargetStamp "$sandbox\guard\stamp.txt" }
    catch { $guardError = $_.Exception.Message }
    Assert-Content ($guardError -match '1\.66\.7.+1\.65\.27') 'A different input APK version must stop the build.'
    $guardError = $null
    try { Assert-ContentExecutable -ApktoolConfig "$sandbox\guard\apktool.yml" -TargetStamp "$sandbox\guard\missing.txt" }
    catch { $guardError = $_.Exception.Message }
    Assert-Content ($guardError -match 'build_mod\.ps1') 'A missing record stamp must point at build_mod.ps1.'
    Write-Host 'Content guard checks passed.'
```

In `work/scripts/test_pipeline.ps1`, after the `$inject` assertions, add:

```powershell
Assert-True ($inject -match 'Assert-ContentExecutable') 'inject_mod.ps1 must refuse input APKs the content record was not verified against.'
```

Run: `.\work\scripts\test_content.ps1`
Expected: FAIL, because `content_guard.ps1` is not found.

- [ ] **Step 2: Implement `work/scripts/content_guard.ps1`**

```powershell
#requires -Version 5.1
# The content record is verified against one executable; never repack a different one with it.
function Assert-ContentExecutable {
    param(
        [Parameter(Mandatory)][string]$ApktoolConfig,
        [Parameter(Mandatory)][string]$TargetStamp
    )
    if (-not (Test-Path -LiteralPath $TargetStamp)) {
        throw "Content record stamp is missing: $TargetStamp. Run work\scripts\build_mod.ps1 first."
    }
    $expected = (Get-Content -Raw -LiteralPath $TargetStamp).Trim()
    $config = Get-Content -Raw -LiteralPath $ApktoolConfig
    if ($config -notmatch "(?m)^\s*versionName:\s*'?([^'\r\n]+?)'?\s*$") {
        throw "versionName was not found in $ApktoolConfig."
    }
    $actual = $Matches[1]
    if ($actual -ne $expected) {
        throw "input\Game.apk is $actual, but the verified content record targets executable $expected. Change mod\content\target.ini only after verifying content for $actual."
    }
}
```

- [ ] **Step 3: Call it from `inject_mod.ps1`**

In `work/scripts/inject_mod.ps1`, add `. "$PSScriptRoot\content_guard.ps1"` after `. "$PSScriptRoot\native.ps1"`. Then insert directly before `# ---- 2. Discover launcher activity ----`:

```powershell
Assert-ContentExecutable -ApktoolConfig (Join-Path $Unpacked 'apktool.yml') -TargetStamp (Join-Path $BuildOut 'cmake\content-executable.txt')
```

- [ ] **Step 4: Verify**

Run: `.\work\scripts\test_content.ps1; .\work\scripts\test_pipeline.ps1; .\work\scripts\inject_mod.ps1`
Expected: `Content guard checks passed.`, the pipeline tests pass, and `inject_mod.ps1` completes with `Final APK: ...`.

- [ ] **Step 5: Commit**

```bash
git add work/scripts/content_guard.ps1 work/scripts/inject_mod.ps1 work/scripts/test_content.ps1 work/scripts/test_pipeline.ps1
git commit -m "Refuse input APKs the content record was not verified for"
```

---

### Task 6: Device acceptance cases

Cases A and F uninstall `com.ea.gp.pvzheroes`, which deletes its local data. Run them on a test device, or after linking the game account.

**Files:**
- Modify: `work/scripts/device.ps1`
- Create: `work/scripts/test_device_content.ps1`

**Interfaces:**
- Consumes: the `device.ps1` functions (Task 1), the Task 4 log lines, `[Verification] SuccessMarker` and `FailurePattern`, `work/build-out/Sprig-newest-content.apk` (Task 1), and the `build_mod.ps1` switches (Task 3).
- Produces: in `device.ps1`, `Install-SprigBuild [-ContentDir <dir>] [-DropTarget] [-LogContentMap]`, `Remove-SprigGame`, and `Wait-SprigOutcome -LogPath [-TimeoutSeconds]`, which returns `Success`, `Crash`, `Failure`, `Disconnected` or `Timeout`.

- [ ] **Step 1: Extend `work/scripts/device.ps1`**

Append:

```powershell
function Install-SprigBuild {
    param([string]$ContentDir, [switch]$DropTarget, [switch]$LogContentMap)
    & "$PSScriptRoot\build_mod.ps1" -ContentDir $ContentDir -DropTarget:$DropTarget -LogContentMap:$LogContentMap
    & "$PSScriptRoot\inject_mod.ps1"
    & "$PSScriptRoot\install.ps1"
}

function Remove-SprigGame {
    $oldPreference = $ErrorActionPreference
    try {
        # adb exits nonzero when the package is not installed, which is fine here.
        $ErrorActionPreference = 'Continue'
        & adb uninstall $SprigPackage 2>&1 | Out-Null
    } finally {
        $ErrorActionPreference = $oldPreference
    }
}

# Crash beats failure beats success when several appear in the same poll.
function Wait-SprigOutcome {
    param([Parameter(Mandatory)][string]$LogPath, [int]$TimeoutSeconds = 1800)
    $crash = $SprigCrashPattern
    $failure = "$(Get-ContentSetting 'FailurePattern')|content: no verified association"
    $success = [regex]::Escape((Get-ContentSetting 'SuccessMarker'))
    return Watch-Logcat -LogPath $LogPath -TimeoutSeconds $TimeoutSeconds -Classify {
        param($text)
        if ($text -match $crash) { 'Crash' }
        elseif ($text -match $failure) { 'Failure' }
        elseif ($text -match $success) { 'Success' }
    }.GetNewClosure()
}
```

- [ ] **Step 2: Create `work/scripts/test_device_content.ps1`**

```powershell
#requires -Version 5.1
<#
.SYNOPSIS
  Runs one on-device acceptance case for verified content selection.
.DESCRIPTION
  Cases A and F uninstall com.ea.gp.pvzheroes, which deletes its local data.
  Run them on a test device or after linking the game account.
#>
param(
    [Parameter(Mandatory)][ValidateSet('A', 'B', 'C', 'D', 'E', 'F')][string]$Case
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\device.ps1"

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$Selected = "Content selected: $(Get-ContentSetting 'AssetVersion')"
$BrokenApk = Join-Path $ProjectRoot 'work\build-out\Sprig-newest-content.apk'

function New-LogPath {
    param([string]$Name)
    Join-Path $ProjectRoot ("work\device-logs\case{0}-{1}-{2:yyyyMMdd-HHmmss}.log" -f $Case, $Name, (Get-Date))
}

function Assert-Run {
    param([string]$LogPath, [string]$Outcome, [string[]]$Present = @(), [string[]]$Absent = @())
    $text = Get-Content -Raw -LiteralPath $LogPath
    $problems = @()
    if ($Outcome -ne 'Success') { $problems += "outcome $Outcome" }
    foreach ($line in $Present) { if ($text -notmatch [regex]::Escape($line)) { $problems += "missing '$line'" } }
    foreach ($line in $Absent) { if ($text -match [regex]::Escape($line)) { $problems += "unexpected '$line'" } }
    if ($problems) { throw "Case $Case failed: $($problems -join '; '). Log: $LogPath" }
}

function Invoke-Launch {
    param([string]$Name, [int]$TimeoutSeconds = 1800)
    Start-SprigGame
    Write-Host 'Accept the download prompt on the device if the game shows one.'
    $log = New-LogPath $Name
    return [pscustomobject]@{ Log = $log; Outcome = (Wait-SprigOutcome -LogPath $log -TimeoutSeconds $TimeoutSeconds) }
}

switch ($Case) {
    'A' {
        # Fresh install downloads the verified target and reaches the home screen first try.
        Remove-SprigGame
        Install-SprigBuild
        $run = Invoke-Launch 'fresh'
        Assert-Run $run.Log $run.Outcome -Present 'content: FindAssociation hook OK', 'content: Read hook OK', 'content: reset hook OK', $Selected -Absent 'Content state reset'
    }
    'B' {
        # A device left on unverified content recovers on the first launch of the fixed build.
        if (-not (Test-Path $BrokenApk)) { throw "Missing $BrokenApk; Task 1 Step 1 saves it." }
        Invoke-Native adb install -r $BrokenApk
        $broken = Invoke-Launch 'broken' 900
        if ($broken.Outcome -eq 'Success') { throw "The newest-content build succeeded, so case B cannot reproduce the stuck state. Log: $($broken.Log)" }
        $stuck = Invoke-Launch 'stuck' 600
        if ($stuck.Outcome -eq 'Success') { throw "The newest-content build recovered by itself. Log: $($stuck.Log)" }
        Install-SprigBuild
        $run = Invoke-Launch 'repaired'
        Assert-Run $run.Log $run.Outcome -Present $Selected
        $reset = Select-String -LiteralPath $run.Log -Pattern 'Content state reset: .*' | Select-Object -First 1
        Write-Host $(if ($reset) { "Repair: $($reset.Matches[0].Value)" } else { 'Repair: tracker held no other version; selection alone recovered.' })
    }
    'C' {
        # A healthy device relaunches without resets or restores.
        $run = Invoke-Launch 'relaunch'
        Assert-Run $run.Log $run.Outcome -Present $Selected -Absent 'Content state reset', 'restored from record'
    }
    'D' {
        # EA removing the target from the live map is survived through the embedded record.
        try {
            Install-SprigBuild -DropTarget
            $run = Invoke-Launch 'dropped'
            Assert-Run $run.Log $run.Outcome -Present 'content: test build dropped', 'restored from record', $Selected
        } finally {
            Install-SprigBuild
        }
    }
    'E' {
        # Offline relaunch after a completed download behaves like the stock game.
        Invoke-Native adb shell cmd connectivity airplane-mode enable
        try {
            $run = Invoke-Launch 'offline' 300
            Assert-Run $run.Log $run.Outcome -Absent 'Content state reset'
        } finally {
            Invoke-Native adb shell cmd connectivity airplane-mode disable
        }
    }
    'F' {
        # A download interrupted by a process kill resumes on the next launch.
        Remove-SprigGame
        Install-SprigBuild
        Start-SprigGame
        Write-Host 'Accept the download prompt on the device within 30 seconds if the game shows one.'
        Wait-LogPattern -LogPath (New-LogPath 'download') -Pattern ([regex]::Escape($Selected)) -TimeoutSeconds 300
        Start-Sleep -Seconds 30
        Invoke-Native adb shell am force-stop $SprigPackage
        $run = Invoke-Launch 'resumed'
        Assert-Run $run.Log $run.Outcome -Present $Selected
    }
}
Write-Host "Case $Case passed."
```

- [ ] **Step 3: Verify the scripts parse**

Run: `.\work\scripts\test_pipeline.ps1`
Expected: pass, with no parse errors for `device.ps1` or `test_device_content.ps1`.

- [ ] **Step 4: Run every case in order and record results**

```powershell
.\work\scripts\test_device_content.ps1 -Case B
.\work\scripts\test_device_content.ps1 -Case C
.\work\scripts\test_device_content.ps1 -Case D
.\work\scripts\test_device_content.ps1 -Case E
.\work\scripts\test_device_content.ps1 -Case A
.\work\scripts\test_device_content.ps1 -Case F
```

Expected: `Case <X> passed.` for each. B runs first because it needs an existing install. A and F last because they uninstall.

Decision rules:
- B fails with the game still loading forever after `Content selected` appeared: stop. Inspect the last `Next screen` line and the download state in the `repaired` log. Extend `ResetTrackedVersions` in `hooks.cpp` to also call `CleanCache()` on the bundle cache instance; find the instance through `PrepareFromStreamingAssetsBootstrapCommand.<BundleCache>k__BackingField`, offset `0x70` in `work/dump/dump.cs`. Rerun B. Update the spec's Hooks section to match.
- E fails because the stock game does not reach the success marker offline: confirm with stock-like behavior (Task 1's `task1-run*.log` show what the game does without the fix), record the offline marker in the spec, and relax case E to match it.
- Any `Crash`: stop and debug with superpowers:systematic-debugging before continuing.

- [ ] **Step 5: Commit**

```bash
git add work/scripts/device.ps1 work/scripts/test_device_content.ps1
git commit -m "Add device acceptance cases for verified content"
```

---

### Task 7: Capture and verification tooling for newer content

**Files:**
- Create: `work/scripts/capture_content_map.ps1`, `work/scripts/verify_content.ps1`

**Interfaces:**
- Consumes: `Install-SprigBuild`, `Start-SprigGame`, `Wait-LogPattern`, `Wait-SprigOutcome`, `ConvertFrom-ContentMapLog` and `Remove-SprigGame` (Tasks 1 and 6). `build_mod.ps1 -LogContentMap` and `-ContentDir` (Task 3).
- Produces: `capture_content_map.ps1 [-OutFile <path>]` and `verify_content.ps1 -AssetVersion <v> -AssociationFile <path> [-Fresh] [-Adopt]`.

- [ ] **Step 1: Create `work/scripts/capture_content_map.ps1`**

```powershell
#requires -Version 5.1
<#
.SYNOPSIS
  Saves EA's live content map (ClientServerContentAssociations) from the connected device.
.DESCRIPTION
  Installs a build that logs the raw map, launches the game, writes the map text,
  then reinstalls the normal build.
#>
param([string]$OutFile)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\device.ps1"

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
if (-not $OutFile) { $OutFile = Join-Path $ProjectRoot 'work\content-map.txt' }
$log = Join-Path $ProjectRoot ("work\device-logs\capture-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))
try {
    Install-SprigBuild -LogContentMap
    Start-SprigGame
    Wait-LogPattern -LogPath $log -Pattern 'content: map text end' -TimeoutSeconds 300
    $text = ConvertFrom-ContentMapLog -LogPath $log
    [System.IO.File]::WriteAllText($OutFile, $text, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Saved the content map to $OutFile"
} finally {
    Install-SprigBuild
}
```

- [ ] **Step 2: Create `work/scripts/verify_content.ps1`**

```powershell
#requires -Version 5.1
<#
.SYNOPSIS
  Tests whether newer EA content runs on Sprig's executable before it is adopted.
.DESCRIPTION
  Builds Sprig targeting -AssetVersion, using -AssociationFile (a minimal map document made
  from capture_content_map.ps1 output in the same format as mod\content\association.txt).
  It installs the build, launches the game, and classifies the run. -Fresh uninstalls first,
  which deletes local game data, to test the new-user path. The normal build is reinstalled
  afterwards, and its reset returns the device to the verified target. -Adopt updates
  mod\content after a passing run.
#>
param(
    [Parameter(Mandatory)][string]$AssetVersion,
    [Parameter(Mandatory)][string]$AssociationFile,
    [switch]$Fresh,
    [switch]$Adopt
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\device.ps1"

$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$ContentDir = Join-Path $ProjectRoot 'mod\content'
$candidate = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-verify-$([guid]::NewGuid())"
$log = Join-Path $ProjectRoot ("work\device-logs\verify-{0}-{1:yyyyMMdd-HHmmss}.log" -f $AssetVersion, (Get-Date))

$ini = Get-Content -Raw (Join-Path $ContentDir 'target.ini')
if ($ini -notmatch '(?m)^[ \t]*AssetVersion[ \t]*=') { throw 'mod\content\target.ini has no AssetVersion.' }
$candidateIni = $ini -replace '(?m)^([ \t]*AssetVersion[ \t]*=[ \t]*)[^\r\n]*', "`${1}$AssetVersion"

try {
    New-Item -ItemType Directory -Force $candidate | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $candidate 'target.ini'), $candidateIni, [System.Text.UTF8Encoding]::new($false))
    Copy-Item -LiteralPath $AssociationFile (Join-Path $candidate 'association.txt')
    if ($Fresh) { Remove-SprigGame }
    Install-SprigBuild -ContentDir $candidate
    Start-SprigGame
    Write-Host 'Accept the download prompt on the device if the game shows one.'
    $outcome = Wait-SprigOutcome -LogPath $log
    $selected = (Get-Content -Raw -LiteralPath $log) -match [regex]::Escape("Content selected: $AssetVersion")
    $passed = $outcome -eq 'Success' -and $selected
    Write-Host ('{0}: content {1} -> {2}. Log: {3}' -f $(if ($passed) { 'PASS' } else { 'FAIL' }), $AssetVersion, $outcome, $log)
    if ($passed -and $Adopt) {
        Copy-Item (Join-Path $candidate 'target.ini') (Join-Path $ContentDir 'target.ini') -Force
        Copy-Item (Join-Path $candidate 'association.txt') (Join-Path $ContentDir 'association.txt') -Force
        Write-Host 'Updated mod\content. Review the diff, run test_content.ps1 and the acceptance cases, then commit.'
    }
} finally {
    Install-SprigBuild
    if (Test-Path $candidate) { Remove-Item -LiteralPath $candidate -Recurse -Force }
}
```

- [ ] **Step 3: Verify both scripts on the device**

Run: `.\work\scripts\test_pipeline.ps1`
Expected: pass, with no parse errors.

Run: `.\work\scripts\capture_content_map.ps1`
Expected: `Saved the content map to ...\work\content-map.txt`, and the file contains the 1.67.8 association text in `mod\content\association.txt`.

Run a negative verification against the known-bad version. Build a one-association document for 1.68.10 from `work\content-map.txt` the same way as Task 1 Step 10, saved as `work\content-1.68.10.txt`:

```powershell
.\work\scripts\verify_content.ps1 -AssetVersion 1.68.10 -AssociationFile .\work\content-1.68.10.txt
```

Expected: `FAIL: content 1.68.10 -> Crash` or `-> Failure`. Afterwards the device is back on the normal build. Relaunching passes case C (`.\work\scripts\test_device_content.ps1 -Case C`), which proves the reset returns the device to the target.

- [ ] **Step 4: Commit**

```bash
git add work/scripts/capture_content_map.ps1 work/scripts/verify_content.ps1
git commit -m "Add content map capture and verification tools"
```

---

### Task 8: Documentation

**Files:**
- Modify: `README.md`, `docs/getting-started.md`, `docs/setup.md`

- [ ] **Step 1: Update `README.md`**

- Line 8: replace `- Selects the newest available content association.` with `- Selects the newest content verified for its executable (\`mod/content/target.ini\`) and repairs devices left on unverified content.`
- Line 22: replace ``confirm `Sprig loaded` and `Latest content association selected`.`` with ``confirm `Sprig loaded` and `Content selected: 1.67.8`.``
- Line 32: replace `It blocks the client update check and uses the newest content association from EA's live map.` with ``It blocks the client update check; `mod/src/content/` selects verified content from EA's live map.``
- Line 34: replace `Sprig selects the newest app/content association in EA's live map while retaining` with ``Sprig selects the newest content verified against 1.65.27 (`mod/content/target.ini`) while retaining``.

- [ ] **Step 2: Update `docs/getting-started.md`**

Replace both paragraphs under `## Included hooks` with:

````markdown
`mod/src/main.cpp` keeps the 1.65.27 executable and metadata compatible with the included dumps. It makes `FailsMinimumAppVersion` return `false` and installs the content hooks in `mod/src/content/`:

- `AssetVersionMap.FindAssociation` returns the association for the target in `mod/content/target.ini`, or the newest one below it. It never returns newer content, so EA releases the executable cannot run are ignored.
- `AssetVersionMapReader.Read` restores the target from `mod/content/association.txt` if EA removes it from the live map.
- `LoadAssetBundleVersionMapCommand.HandleDownloadRequestSuccess` clears the game's tracked content versions when they name another version, so a device left on unverified content recovers on its next online launch.

Run `adb logcat -s Sprig` after launch. It should show `content: FindAssociation hook OK` and `Content selected: 1.67.8`. `Newer content ... available (unverified)` means EA published content that has not been verified for this executable.

### Adopting newer content

1. `.\work\scripts\capture_content_map.ps1` saves EA's live map to `work\content-map.txt`.
2. Copy the new association into a minimal document in the same format as `mod/content/association.txt`.
3. `.\work\scripts\verify_content.ps1 -AssetVersion <version> -AssociationFile <file> -Fresh` builds, installs, and classifies a run. `-Fresh` deletes local game data. Add `-Adopt` to update `mod/content` when it passes.
4. Run `.\work\scripts\test_content.ps1` and `.\work\scripts\test_device_content.ps1` cases A to F, then commit.
````

In `## Troubleshooting`, replace the `“Connection error”` bullet with:

```markdown
- “Connection error” with `Version ... not found in provided map` in Unity logcat: check `adb logcat -s Sprig` for `content: FindAssociation hook OK`. If it is missing, the hook did not load. `content: no verified association` means neither EA's map nor `mod/content/association.txt` provided the target.
- Endless loading after an older build selected newer content: launch once while online. `Content state reset` in `adb logcat -s Sprig` shows the repair ran.
- The build stops with `the verified content record targets executable`: `input/Game.apk` is not the executable named in `mod/content/target.ini`.
```

- [ ] **Step 3: Update `docs/setup.md`**

Replace `Keep PvZH 1.65.27 at \`input/Game.apk\` to match the dumps. Sprig's content-map hook selects the newest advertised app/content association without upgrading the executable or metadata.` with:

```markdown
Keep PvZH 1.65.27 at `input/Game.apk` to match the dumps and the verified content record in `mod/content/target.ini`; the build stops if they differ. Sprig selects the newest content verified for that executable without upgrading the executable or metadata.
```

- [ ] **Step 4: Verify that no stale wording remains**

Run: `Select-String -Path README.md, docs\*.md -Pattern 'Latest content association|newest available content|newest advertised'`
Expected: no matches.

- [ ] **Step 5: Commit**

```bash
git add README.md docs/getting-started.md docs/setup.md
git commit -m "Document verified content selection"
```
