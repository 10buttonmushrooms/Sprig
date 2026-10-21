# Debugging

> **Rules**
> - Read `adb logcat -s Sprig` first: every module logs whether its lookups and hooks worked.
> - Map crash addresses in `libil2cpp.so` with `.\sprig.ps1 find --rva <0xADDRESS>`.
> - Change one thing at a time and rebuild; report the log lines you saw.

## Logs

```powershell
adb logcat -c                      # clear old lines, then launch the game
adb logcat -s Sprig                # Sprig and every Bloom module (LOG_INFO, LOG_WARN, LOG_ERR)
adb logcat -s Sprig Unity          # also the game's own messages
adb logcat -b crash                # native crashes and their backtraces
```

`Sprig loaded` means the built-in hooks installed. Each module should log one line from `Init`
saying what it installed (for example `UnlockAll: installed (fixup=1 update=1 ...)`).

## The mod does nothing

| Check | How |
| --- | --- |
| The module was built | `build_mod.ps1` prints `-- Bloom: <Name> (PnP)`; `sprig lint` reports a broken `bloom.ini` |
| `Init` ran | add `LOG_INFO("MyMod: Init")` at its top |
| Every lookup worked | each `IsValid()` failure should `LOG_ERR`; a wrong name is a `names` lint error |
| The hook fires | `LOG_INFO` inside the hook; if it never logs, the method may be inlined: hook a caller ([hooks.md](hooks.md#hooks-that-never-fire)) |
| The game uses your new `$type` | log from its methods ([card-dsl.md](card-dsl.md#finding-out-how-the-engine-uses-something)) |

## The game crashes

1. Run `adb logcat -b crash` (or `adb bugreport` for tombstones on a phone without root).
2. Find the backtrace lines `#00 pc 000000000228c470  .../libil2cpp.so`.
3. Map each `libil2cpp.so` address: `.\sprig.ps1 find --rva 0x228c470` names the method. The first
   game method in the backtrace that you hook (or call) is where to look.
4. A `libsprig.so` frame is your code. Its symbols are stripped; add `LOG_INFO` lines around the
   suspect code to narrow it down.

Common causes, all checked by `sprig lint`: a hook signature that does not match its method
(`hook-signature`), calling a null `original` (`original`), a lookup at file scope (`init-phase`), a
managed object kept in a global (`managed-global`), and an injected class smaller than its base
(`custom-class`).

## Building

`build_mod.ps1` stops on lint errors before compiling; `-NoLint` builds anyway. Compiler errors
print as `path:line:col: error: ...`: fix the first one first, then rebuild.
