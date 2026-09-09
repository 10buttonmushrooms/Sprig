# Setup

Sprig runs on Windows and builds a modified Android APK for arm64 devices.

## Prerequisites

Install these tools and ensure each is available on your `PATH`:

- PowerShell 5.1 or newer
- Git
- Java 17 or newer
- CMake
- Ninja
- Android SDK Platform Tools (`adb`)
- Android NDK

Set `ANDROID_NDK_ROOT` to the NDK directory that contains `build/cmake/android.toolchain.cmake`:

```powershell
[Environment]::SetEnvironmentVariable('ANDROID_NDK_ROOT', 'C:\Android\ndk\android-ndk-r27d', 'User')
```

Open a new PowerShell window after setting it. To set it only for the current window instead:

```powershell
$env:ANDROID_NDK_ROOT = 'C:\Android\ndk\android-ndk-r27d'
```

You also need an internet connection for the first build. Sprig downloads its pinned BNM-Android, Dobby, apktool, and uber-apk-signer dependencies automatically.

## Game APK

Keep PvZH 1.65.27 at `input/Game.apk` to match the dumps. Sprig's content-map hook selects the newest advertised app/content association without upgrading the executable or metadata.

## Android device

Use an arm64 Android device with Developer options and USB debugging enabled. Connect it by USB, accept the computer's debugging prompt on the device, then confirm it is visible:

```powershell
adb devices
```

The device should appear with the status `device`.

## Build and install

From the repository root, run:

```powershell
.\work\scripts\all.ps1
```

This downloads dependencies, builds `libsprig.so`, injects it into the APK, signs the result, and installs it. The finished APK is written to `out/Sprig.apk`.

## Verify

Unlock the device and launch PvZH. To confirm the hooks loaded, run:

```powershell
adb logcat -s Sprig
```

## Important: existing installs and saves

Sprig signs the APK with a different certificate. Android may reject installation over an official copy with `INSTALL_FAILED_UPDATE_INCOMPATIBLE`. Back up any local game data before removing the existing app; uninstalling it can erase local saves.

```powershell
adb uninstall com.ea.gp.pvzheroes
```
