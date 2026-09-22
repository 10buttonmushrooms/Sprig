# Bloom

Bloom is Sprig's module layer. It keeps reusable code and drop-in features outside the base mod while still producing one `libsprig.so`.

There are two module types:

- **Core** modules are library-like building blocks. Sprig compiles their sources and adds their include directories, but does not run them automatically. Other mod code explicitly includes and calls them.
- **PnP** modules are plug-and-play features. They compile into Sprig, register an initializer, and run automatically after BNM reports that IL2CPP is loaded. Users configure them through the module's `config.ini` instead of editing C++.

## Install a module

Place the module directory directly under `Bloom/`:

```text
Bloom/
  BundleForcer/
    bloom.ini
    main.cpp
    config.ini
    inc/
    src/
    bundles/
```

The folder name is the module id. The normal Sprig build discovers modules automatically, so no central source list needs editing.

Every module has a `bloom.ini` manifest:

```ini
[bloom]
type = pnp
```

Use `type = core` for a Core module.

## Core modules

Core modules may contain:

```text
MyLibrary/
  bloom.ini
  inc/
    bloom/
      my_library.hpp
  src/
    my_library.cpp
```

All C++, C++ header, and BNM/Dobby dependencies still come from the main Sprig target. Source files under `src/` are compiled automatically. `inc/` and `include/` are added to Sprig's include path.

Nothing in a Core module runs automatically. Include its public header from Sprig or another module and call it like a normal library.

## PnP modules

PnP modules require `main.cpp` and `config.ini`. Source files under `src/` are also compiled automatically.

A minimal entry point is:

```cpp
#include <bloom/bloom.hpp>

namespace {

void Initialize() {
    if (!BLOOM_CONFIG_BOOL("module", "enabled", true)) {
        return;
    }

    // Install hooks here.
}

}

BLOOM_REGISTER_PNP(Initialize);
```

Bloom assigns `BLOOM_MODULE_ID` from the directory name at compile time, so modules do not need to hard-code their own name.

PnP initializers run once, in module-id order, from Sprig's existing `OnIl2CppLoaded` path after Sprig's built-in hook setup is attempted. BNM is ready at that point. A base-hook lookup failure does not stop Bloom from initializing.

## Configuration

PnP `config.ini` files are embedded into `libsprig.so` during the CMake configure step. Editing a config and rebuilding Sprig changes the module without editing its source.

Available helpers are:

```cpp
BLOOM_CONFIG("section", "key")
BLOOM_CONFIG_BOOL("section", "key", fallback)
BLOOM_CONFIG_INT("section", "key", fallback)
```

The parser supports INI sections, `key = value` pairs, blank lines, and lines beginning with `#` or `;`.

Because configuration is embedded, changing `config.ini` after the APK is already installed does not change the running module; rebuild and reinstall Sprig after an edit.

## Bundles and module assets

If a module contains `bundles/`, `inject_mod.ps1` copies its contents into:

```text
assets/sprig/bloom/<ModuleId>/bundles/
```

inside the rebuilt APK. The staging step removes the previous Bloom asset directory first, so deleted modules or bundles do not remain in the cached apktool tree.

Bloom only stages the files. A module that needs bundle bytes must still load or extract the APK asset using the Android/Unity mechanism appropriate for that module.

## Templates

Copy one of the ignored templates as a starting point:

```powershell
Copy-Item -Recurse Bloom\_templates\PnP Bloom\MyModule
Copy-Item -Recurse Bloom\_templates\Core Bloom\MyLibrary
```

Then edit the copied module. Directories beginning with `_` are ignored by module discovery.
