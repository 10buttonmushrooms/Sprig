#include <bloom/bloom.hpp>

#include "log.h"

namespace {

void Initialize() {
    if (!BLOOM_CONFIG_BOOL("module", "enabled", true)) {
        LOG_INFO("Bloom module disabled by config");
        return;
    }

    // Register hooks or other automatic behavior here.
}

} // namespace

BLOOM_REGISTER_PNP(Initialize);
