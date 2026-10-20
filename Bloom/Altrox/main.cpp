#include <bloom/bloom.h>
#include <bundle_forcer.h>

static void Init() {
    if (!BLOOM_CONFIG_BOOL("General", "Enabled", true)) return;
    BundleForcer::Force(BLOOM_MODULE_NAME, "bundles");
    if (BLOOM_CONFIG_BOOL("Altrox", "FiftyHpAddon", false)) BundleForcer::Force(BLOOM_MODULE_NAME, "fifty-hp");
}

BLOOM_REGISTER_PNP(Init);
