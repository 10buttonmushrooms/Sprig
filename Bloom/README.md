# Bloom

Copy `_templates/Core` or `_templates/PnP` into `Bloom/<Name>`, then build
Sprig normally. Names use letters, digits, `_`, and `-`, starting with a letter.
Templates and directories without a valid `bloom.ini` are ignored.

```text
Bloom/<Name>/
  bloom.ini       # [Bloom] Type=Core or Type=PnP
  main.cpp        # optional
  inc/            # optional public headers (include/ also works)
  src/            # optional C++ sources, searched recursively
  config.ini      # PnP build-time settings; optional, defaults if absent
  bundles/        # example asset folder; choose any folder name
```

`Core` provides headers and sources for explicit calls from Sprig or other
modules. Bloom schedules no Core initializer. `PnP` registers a `void Init()`:

```cpp
#include <bloom/bloom.h>

static void Init() {
    if (!BLOOM_CONFIG_BOOL("General", "Enabled", true)) return;
    auto text = BLOOM_CONFIG("General", "Label");
    int priority = BLOOM_CONFIG_INT("General", "Priority", 0);
    // Install hooks here; BNM/IL2CPP is ready.
}
BLOOM_REGISTER_PNP(Init);
```

Initializers run once after Sprig attempts its built-in hooks, even if a built-in
lookup fails. Register once per module; there is no dependency ordering. A thrown
C++ exception is logged and other initializers continue. Registration is for
static startup, before initialization; repeated or recursive initialization is a no-op.

CMake defines `BLOOM_MODULE_NAME` (folder name string) and `BLOOM_MODULE_TYPE`
(`Bloom::Type::Core` or `Bloom::Type::PnP`) for every module source. Include
`<bloom/bloom.h>` to use the type. `Bloom::Modules()` lists identities and embedded
config; `Bloom::FindModule(name)` returns an identity pointer or `nullptr`.
Sprig/other modules can explicitly call `Bloom::Config(name, section, key)` and
the corresponding `ConfigBool`/`ConfigInt` functions.

Config section/key names are case-sensitive. Whitespace, UTF-8 BOM, CRLF, and
whole-line `;`/`#` comments are supported. Values are literal trimmed text;
quotes and inline comment characters remain part of the value. The last duplicate
key wins. Missing strings are empty; missing/invalid booleans or decimal integers
use the supplied default. Booleans accept true/false, yes/no, on/off, and 1/0,
ignoring case. Rebuild after edits; configs are embedded in `libsprig.so`.

Every immediate module directory except `inc/`, `include/`, and `src/` is staged
under `assets/sprig/bloom/<Name>/<directory>/` during APK injection. For example,
`Bloom/BundleForcer/bundles/a.bin` becomes
`assets/sprig/bloom/BundleForcer/bundles/a.bin`. Root code/config files are not
assets. Bloom only stages files; the module handles Android asset loading.
Removing modules/assets and rebuilding removes their staged copies.
