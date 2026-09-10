# Sprig

Sprig is a minimal native modding base for the Android version of Plants vs. Zombies Heroes. It builds a C++ library, inserts it into the bundled APK, signs the APK, and installs it on an arm64 Android device.

## Included mods

- Bypasses the minimum app-version check.
- Selects the newest available content association.

## Setup

Install Git, Java 17+, CMake, Ninja, Android SDK Platform Tools (`adb`), and the Android NDK. Set `ANDROID_NDK_ROOT` to the NDK directory, then:

```powershell
.\work\scripts\all.ps1
```

The build script downloads the exact BNM and Dobby revisions Sprig expects.

See the [setup guide](docs/setup.md) for the full prerequisite, device, install, and verification steps.

Unlock the device and launch PvZH. Use `adb logcat -s Sprig` to confirm `Sprig loaded` and `Latest content association selected`. The output APK is `out/Sprig.apk`. Re-signing changes its certificate, so replacing an official installation may require uninstalling it first, which can erase local save data.

## Learn

- [Beginner guide](docs/getting-started.md): architecture, first hook, and troubleshooting
- [PvZH API](docs/reference/api.md): useful classes and hook targets
- [Type index](docs/reference/types.csv): searchable list of 5,834 `PvZCards.*` types
- [Enums](docs/reference/enums.md), [cards](docs/reference/cards.csv), and [heroes](docs/reference/heroes.md)

Write mod code in [`mod/src/main.cpp`](mod/src/main.cpp). It blocks the client update check and uses the newest content association from EA's live map.

The APK base and bundled references target 1.65.27 and were distilled from [`PvZH-Decomp@dda825d`](https://github.com/10buttonmushrooms/PvZH-Decomp/tree/dda825d568c6a5c866b5eaec0ea26bdbdf94f1de). Sprig selects the newest app/content association in EA's live map while retaining the original executable and metadata for dump compatibility.
