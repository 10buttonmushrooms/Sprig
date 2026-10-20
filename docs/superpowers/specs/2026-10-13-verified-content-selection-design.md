# Verified content selection

Date: 2026-10-13
Status: design for review. Written while the owner was away, at their request to
design without further questions.

## Problem

After EA published content 1.68.10, a fresh Sprig install crashes and the next
launch loads forever.

Evidence from `out/Sprig.apk` on an SM-S948B (Android 16), 2026-10-13:

```text
I Sprig : Sprig loaded
I Sprig : Latest content association selected
I Unity : [MAPPING]:Using asset bundle version 1.68.10 for app version 1.65.27
          with server version:cd332b629f26e76e09ce720ff70170c1
I Unity : [LOG] : Next screen GenericMessageBox      (643 MB download prompt)
...       "Please connect to the internet to download necessary files."
```

The device was online throughout; other HTTPS requests from the same process
returned 200 during the failure.

### Root cause

`UseLatestContent` in `mod/src/main.cpp` replaces
`AssetVersionMap.FindAssociation` with
`FindLastGoodAssociation("2147483647.2147483647.2147483647.2147483647")`. That
always picks the newest association in EA's live map. The policy was safe only
while every advertised content version stayed compatible with the 1.65.27
executable. The owner has confirmed that 1.65.27 runs content up to and
including 1.67.8. 1.68.10 is the first incompatible release, so Sprig now
sends every install to content the executable cannot run.

The Bloom commit (`3fd7475`) is not involved. With no modules installed,
`Bloom::InitializePnP` iterates an empty list.

### Why devices stay stuck

The game persists content progress through `PvZCards.Game.AssetBundleVersionTracker`
(PlayerPrefs keys `LastKnownAssetVersion`, `LastKnownBundleVersion`,
`LastKnownServerVersion`, `StartedDownloadAllBundlesMark`,
`PassedDownloadAllBundlesMark`, plus `Ftue`-prefixed copies). The bundle cache
(`HeroesAssetBundleCache`) is versioned per bundle and staged/committed. After a
failed 1.68.10 attempt, those records describe a content version the executable
cannot finish loading. Every later launch still selects 1.68.10, so the device
never leaves that state. Which of the two (tracked versions or cache contents)
blocks the next launch has not been captured yet; the design repairs the tracked
versions and verifies whether that is sufficient (see Testing, case B).

## Goals

These are the guarantees, stated so each one can be tested.

1. **Only verified content.** Sprig never selects content that has not been
   verified against the executable it ships with. The decision is made before
   any bundle download starts, from data already on the device and the live map.
2. **Deterministic.** The same executable and record always select the same
   content version, whatever EA adds to the live map. EA shipping 1.69 or later
   requires no action and changes nothing on devices.
3. **Independent of map removal.** If EA removes the verified association from the
   live map, Sprig restores it from an embedded copy, so the game sees the same
   association it would have downloaded.
4. **Self-repair.** A device whose tracked content version differs from the
   verified target (for example one stuck on 1.68.10) is repaired on the next
   online launch through the game's own `ClearAllTrackedVersions()`. Account and
   save data are not touched.
5. **Graceful degradation.** Every step in Sprig's content path has a defined
   fallback, logs what happened, and never dereferences a missing object. Calls
   into managed code go through `BNM::TryInvoke`.
6. **Build-time refusal.** The build fails if the input APK is not the executable
   the record was verified against, or if the record is missing or malformed.

## Limits

No client-side design can guarantee the following, so the design does not claim
to:

- EA's game servers, login, or CDN being unavailable. PvZH needs them online
  regardless of Sprig.
- EA deleting the verified content's bundles from its CDN. The selection still
  works, but a new install cannot download content that no longer exists.
  Mitigation if it ever happens: ship the verified bundle set inside the APK.
  The game already supports streaming-asset bundles through
  `PrepareFromStreamingAssetsBootstrapCommand`, at a cost of about 650 MB of APK
  size. Not part of this design.
- EA's servers refusing the verified association's `ServerVersion`.
- A first launch with no network. The game must download content once.

## Why not detect compatibility at runtime

Any rule that admits unverified content (for example "newest bundles built with
the same Unity version") can say "compatible" for content that still breaks
the client, because content can add game types the 1.65.27 metadata does not
have. One false positive reproduces today's failure on every device at once.
The owner requires first-try success, so the runtime only ever selects content
that a person has verified on a device. Adopting newer content becomes a
scripted, opt-in developer step (see Adopting newer content). It is never a
runtime guess.

## Design

### Content record

One source of truth, checked into the repo:

```text
mod/content/target.ini
  [Executable]
  AppVersion=1.65.27
  [Content]
  AssetVersion=1.67.8
  [Verification]
  SuccessMarker=Next screen <home screen name>   (device tooling only)
  FailurePattern=<regex for the game's error screens> (device tooling only)

mod/content/association.txt
  A minimal, valid ClientServerContentAssociations document that contains only
  the 1.67.8 association, captured verbatim from EA's live map.
```

`mod/cmake/ContentRecord.cmake` loads and validates both files. It defines no
targets, so it also runs in `cmake -P` host tests. `mod/cmake/Content.cmake` uses
it at configure time, generates `content-record.cpp` with the values as
constants, and writes `content-executable.txt` for the build guard. The association
text is embedded with the same hex-escape technique `Bloom.cmake` uses. The
build fails with a clear message if a file is missing, a key is absent or
duplicated, `AssetVersion` is not a dotted decimal version, or
`association.txt` does not contain the `AssetVersion` string.

### Components

| Unit | Responsibility | Depends on |
| --- | --- | --- |
| `mod/include/content/policy.h`, `mod/src/content/policy.cpp` | Pure C++: parse and compare dotted versions, choose an association from a list of asset versions, decide whether tracked state needs a reset, find the newest unverified version for logging. | Standard library only. Builds and tests on the host. |
| `mod/include/content/record.h` + generated `content-record.cpp` | Expose `Content::GetRecord()`, returning a `Content::Record` with `appVersion`, `assetVersion` and `association`. | CMake |
| `mod/src/content/hooks.cpp` | BNM glue: resolve classes and methods, install the three hooks below, translate IL2CPP objects into policy inputs. | BNM, policy, record, `log.h` |
| `mod/src/main.cpp` | Keep the minimum-version hook. Replace `UseLatestContent` with `Content::InstallHooks()`. | hooks |
| `work/scripts/inject_mod.ps1` | Build guard: apktool's `versionName` must equal `AppVersion` from `target.ini`. | `target.ini` |
| `work/scripts/test_content.ps1` | Host test of the policy and of `Content.cmake` validation, in the same style as `test_bloom.ps1`. | CMake, Visual Studio 2022 |
| `work/scripts/verify_content.ps1` | Developer tool for adopting newer content (see below). | adb, build scripts |

### Hooks

All three are resolved in `Content::InstallHooks()`, which runs from
`OnIl2CppLoaded` before any managed code. Each logs `hook OK` or the exact
lookup that failed.

1. **`AssetVersionMapReader.Read(string text, ChannelLogger logger)`** (static).
   Call the original. If the returned map contains no association whose
   `AssetVersion` equals the target, call the original again on the embedded
   `association.txt`, and `AddAssociation` each association from that second map
   into the first. Log `Content map: N associations` and, when it applies,
   `Content target A missing from live map; restored from record`.
   Because the game's own parser builds the association, no object is built by
   hand.
2. **`AssetVersionMap.FindAssociation(string appVersion)`**. Read the
   `Associations` list, pass each `AssetVersion` to
   `Content::SelectAssociation`, and return the chosen association. Policy:
   exact target first; otherwise the newest association whose version is less
   than or equal to the target (the owner confirmed everything up to 1.67.8
   works); otherwise the original `FindAssociation` result, logged as an error.
   Log `Content selected: 1.67.8`, and
   `Newer content 1.68.10 available (unverified)` when the map has something
   newer. Equal versions resolve to the first in map order.
3. **`LoadAssetBundleVersionMapCommand.HandleDownloadRequestSuccess(string)`**.
   Before calling the original, read the command's `AssetBundleVersionTracker`,
   call `GetLastKnownAssetVersion()`, and ask
   `Content::NeedsTrackedVersionReset(lastKnown, target)`. That returns true only
   when `lastKnown` is non-empty and differs from the target. If true, call
   `ClearAllTrackedVersions()` and log
   `Content state reset: tracked 1.68.10, target 1.67.8`. Then call the original.

The stack captured on 2026-10-13 shows the map being handled inside
`HandleDownloadRequestSuccess`, so hook 3 is expected to run before hooks 1 and
2. Evidence item 2 confirms the order before implementation. Hook 3 resets
against the target rather than against the final selection because the selection
is the target in every case except a broken record, which the build guard
prevents.
An unneeded reset is still safe: the game re-audits cached bundles and skips the
ones whose versions already match.

### Launch sequence

```text
onCreate -> System.loadLibrary("sprig") -> JNI_OnLoad -> il2cpp_init
  -> OnIl2CppLoaded: InstallBuiltInHooks (minimum version), Content::InstallHooks, Bloom PnP
game downloads ClientServerContentAssociations.txt
  -> HandleDownloadRequestSuccess hook: reset tracked versions if they != target
    -> original handler
      -> Read hook: live map + restored target if missing
      -> FindAssociation hook: select target
      -> game records versions, downloads/validates bundles for the target only
```

No bundle download can begin before the FindAssociation hook has returned the
verified target.

### Fallback table

| Failure | Behavior | Log |
| --- | --- | --- |
| `Read` hook lookup fails | Live map is used unchanged. Selection still picks the newest version less than or equal to the target that is present. | `ERROR content: Read hook unavailable` |
| Embedded association parse yields nothing | Same as above. | `ERROR content: record association did not parse` |
| `FindAssociation` hook lookup fails | Stock behavior, which leads to the game's own "version not found" error. Prevented in practice: the executable is pinned by the build guard, so lookups are deterministic, and acceptance case A asserts the hook line. | `ERROR content: FindAssociation hook unavailable` |
| No association at or below the target, even after restore | Return the original result and log a fatal-level line naming both versions. | `FATAL content: no verified association` |
| Tracker unreadable, or a managed exception during reset | Skip the reset and continue the launch. | `WARN content: reset skipped (...)` |
| Managed exception in any hook | Caught by `BNM::TryInvoke`; fall back to the original method's result. | `ERROR content: <hook> threw <class>: <message>` |

## Evidence to capture before implementation

These facts need the device. The plan's first task captures them. Each has a
defined consequence, so none is left open.

1. **Map text format and the 1.67.8 association.** Capture
   `ClientServerContentAssociations.txt` through a temporary hook on `Read`
   (plan Task 1; later captures use `capture_content_map.ps1`). Save the
   minimal document for 1.67.8 as `mod/content/association.txt`. Confirm that
   1.67.8 is present and record its `ServerVersion`. If 1.67.8 is absent from
   the live map, the record is built from the most recent cached copy on the
   test device, and acceptance case D becomes mandatory before release.
2. **Tracker call order.** Log entry to `HandleDownloadRequestSuccess`, `Read`,
   `FindAssociation`, `GetLastKnownAssetVersion`, `RecordCurrentVersions` and
   `ClearAllTrackedVersions` on the failing first run and the stuck relaunch
   (plan Task 1). Fresh-install and healthy-relaunch order are then visible in
   acceptance cases A and C. If
   `GetLastKnownAssetVersion` is consulted before `HandleDownloadRequestSuccess`
   on the online path, move the reset to the earliest caller that has the tracker
   instance and record that call site here.
3. **Success marker.** The `Next screen <Name>` line logged when the main menu is
   reached, captured on the first run of the fixed build (plan Task 4) and stored
   as `[Verification] SuccessMarker`. `verify_content.ps1` and the acceptance
   tests use it as the pass condition.

## Testing

### Host (CI, every push)

`test_content.ps1` builds `policy.cpp` together with a check program
(Visual Studio 2022 x64, like `test_bloom.ps1`) and asserts:

- Version parsing accepts `1.67.8` and `10.0.0`. It rejects empty, `1..2`,
  `1.2.x`, `+1.2`, overflow, and `2147483648.0`.
- Comparison is numeric: `1.67.10 > 1.67.8` and `1.68.0 > 1.67.99`.
- Selection given `[1.66.7, 1.67.8, 1.68.10]` with target `1.67.8` returns
  `1.67.8`. Given `[1.66.7, 1.68.10]` it returns `1.66.7`. Given `[1.68.10]` it
  returns none. Malformed entries are ignored. For distinct versions, selection
  is independent of input order. Equal versions (`1.67.8`, `1.67.8.0`) resolve
  to the first in map order.
- Reset decision: `("", T)` is false, `(T, T)` is false, `("1.68.10", T)` is
  true, and malformed tracked values are true.
- `Content.cmake` fails configuration for: missing files, missing or duplicate
  keys, a bad version, and an association that does not mention the version.

`test_pipeline.ps1` gains static assertions: `inject_mod.ps1` compares
`versionName` to `target.ini`, and `main.cpp` no longer contains
`2147483647`.

### Device acceptance (before release; adb, scripted where possible)

| Case | Setup | Pass condition |
| --- | --- | --- |
| A. Fresh install | Uninstall, then install the fixed build. | `Content selected: 1.67.8`; the download completes; the success marker appears; no crash. |
| B. Stuck device | Install the current (broken) build, let it fail on 1.68.10, relaunch once to confirm the stuck state, then install the fixed build over it. | The first launch of the fixed build reaches the success marker with no data clear. `Content state reset: tracked ...` appears whenever the tracker held another version. If it stays stuck, capture the last `Next screen`/download state and extend the repair to `HeroesAssetBundleCache.CleanCache()` before release. |
| C. Healthy relaunch | Relaunch after A. | No reset line; no download; success marker. |
| D. Target missing from map | Debug-only build flag `SPRIG_TEST_DROP_TARGET` removes the target from the parsed map before restore. | `restored from record`, then the same result as A. |
| E. Offline relaunch | Airplane mode after A. | Game plays offline, the same as stock behavior. |
| F. Interrupted download | Kill the process mid-download during A, then relaunch. | Resumes and reaches the success marker. |

## Adopting newer content

Optional. Sprig keeps working without this step.

`work/scripts/verify_content.ps1 -AssetVersion <v>`:

1. Builds Sprig with the target overridden to `<v>`, using an association
   captured from the live map by the same capture path as Evidence item 1.
2. Installs it on the connected device, launches, and watches logcat for a crash
   (`Fatal signal`, `FATAL EXCEPTION`), an error screen (`GenericErrorMsgBox`, or
   `GenericMessageBox` after the download starts), or the success marker. The
   timeout is long enough for a full download.
3. Reinstalls the normal build. Its reset brings the device back to the verified
   target.
4. Prints PASS or FAIL. On PASS with `-Adopt`, it rewrites `target.ini` and
   `association.txt` for review and commit.

## Documentation changes

- `README.md`: "Selects the newest available content association" becomes
  "Selects the newest content verified for its executable
  (`mod/content/target.ini`) and repairs devices left on unverified content".
- `docs/getting-started.md`: replace the `FindLastGoodAssociation` description
  and the `Latest content association selected` troubleshooting line with the
  new log lines, and describe recovery for stuck devices.
- `docs/setup.md`: point to `mod/content/target.ini` and `verify_content.ps1`.
