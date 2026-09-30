#include <bloom/bloom.h>

static void Init() {
    if (!BLOOM_CONFIG_BOOL("General", "Enabled", true)) return;
    // BNM/IL2CPP is ready. Install this module's hooks here.
}

BLOOM_REGISTER_PNP(Init);
