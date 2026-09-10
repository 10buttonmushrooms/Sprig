# Getting started

This guide assumes you know C++ but nothing about Android or IL2CPP modding.

## Architecture

PvZH is a Unity game compiled with IL2CPP: its original C# code became native code in `libil2cpp.so`, while `global-metadata.dat` retained information about classes and methods.

Sprig adds one native library to the APK:

1. `inject_mod.ps1` unpacks the APK with apktool.
2. It copies `libsprig.so` into `lib/arm64-v8a/`.
3. It adds `System.loadLibrary("sprig")` to the launcher activity.
4. Android calls `JNI_OnLoad` when the library loads.
5. BNM waits for Unity's `il2cpp_init`, then calls `OnIl2CppLoaded`.
6. Your hooks are registered there, after IL2CPP metadata is ready.

Dobby installs native trampolines. BNM provides name-based access to IL2CPP classes, methods, and fields. Never use BNM before `OnIl2CppLoaded`.

## Build and install

Connect an arm64 device with USB debugging enabled and run:

```powershell
.\work\scripts\all.ps1
```

That downloads pinned BNM, Dobby, and APK tools, applies the compatibility patches, builds Sprig, repacks and signs your `input/Game.apk`, and runs `adb install -r`.

Individual stages are also available:

```powershell
.\work\scripts\build_mod.ps1
.\work\scripts\inject_mod.ps1
.\work\scripts\install.ps1
```

## Included hooks

`mod/src/main.cpp` keeps the 1.65.27 executable and metadata compatible with the included dumps while handling EA's newer content map. It makes `FailsMinimumAppVersion` return `false`, then replaces `AssetVersionMap.FindAssociation` with `FindLastGoodAssociation` using a maximum version. That selects the newest association returned by the live map.

Run `adb logcat -s Sprig` after launch. It should show `Latest content association selected`. New content still has to be compatible with the old client.

## Common areas

- Localization: change displayed text safely while learning.
- Effects: observe or alter battle behavior through `EffectRouter`.
- Cards: intercept loading through `CardDataLoaderAndMapper` or storage through `CardDataMapper`.
- Assets: redirect sprites, prefabs, text, or audio through `AssetBundleManager`.
- Battles: inspect game initialization and state-update signals.
- Inventory: inspect local inventory holders and modification signals. Avoid server-owned or synchronized values until you understand their lifecycle.

Search [types.csv](reference/types.csv) for exact namespaces and [the API reference](reference/api.md) for useful signatures.

## Troubleshooting

- “Connection error” with `Version ... not found in provided map` in Unity logcat: check for `Latest content association selected` in `adb logcat -s Sprig`. If it is missing, the hook did not load.
- `ANDROID_NDK_ROOT is not set`: point it at the NDK directory containing `build/cmake/android.toolchain.cmake`.
- `No hooking software` or Dobby compiler errors: `build_mod.ps1` applies the tracked compatibility patches. Dirty BNM/Dobby submodules are expected afterward.
- `libdobby.so not found`: Sprig must link `dobby_static`, not the shared Dobby target.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`: the installed app uses another signing key. Back up anything important before uninstalling it.
- The JNI log appears but `Sprig loaded` does not: unlock the device and launch the game again so Unity reaches `il2cpp_init`.
- The game crashes after adding a hook: check the namespace, name, overload count, return type, parameter types, and whether it is an instance method; then inspect `adb logcat` and the newest Android tombstone.
- Changes do not appear: confirm `input/Game.apk` is the expected APK, then rebuild; the unpack cache refreshes when its SHA-256 changes.
