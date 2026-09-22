#pragma once

#include <string_view>

namespace sprig::bloom {

using InitFunction = void (*)();

bool RegisterPnP(std::string_view module, InitFunction init);
void InitializePnP();

std::string_view ConfigValue(
    std::string_view module,
    std::string_view section,
    std::string_view key
);

bool ConfigBool(
    std::string_view module,
    std::string_view section,
    std::string_view key,
    bool fallback = false
);

int ConfigInt(
    std::string_view module,
    std::string_view section,
    std::string_view key,
    int fallback = 0
);

} // namespace sprig::bloom

#ifndef BLOOM_MODULE_ID
#define BLOOM_MODULE_ID ""
#endif

#define SPRIG_BLOOM_JOIN_INNER(a, b) a##b
#define SPRIG_BLOOM_JOIN(a, b) SPRIG_BLOOM_JOIN_INNER(a, b)

#define BLOOM_REGISTER_PNP(INIT_FN) \
    namespace { \
    [[maybe_unused]] const bool SPRIG_BLOOM_JOIN(_sprig_bloom_registration_, __COUNTER__) = \
        ::sprig::bloom::RegisterPnP(BLOOM_MODULE_ID, (INIT_FN)); \
    }

#define BLOOM_CONFIG(SECTION, KEY) \
    ::sprig::bloom::ConfigValue(BLOOM_MODULE_ID, (SECTION), (KEY))

#define BLOOM_CONFIG_BOOL(SECTION, KEY, FALLBACK) \
    ::sprig::bloom::ConfigBool(BLOOM_MODULE_ID, (SECTION), (KEY), (FALLBACK))

#define BLOOM_CONFIG_INT(SECTION, KEY, FALLBACK) \
    ::sprig::bloom::ConfigInt(BLOOM_MODULE_ID, (SECTION), (KEY), (FALLBACK))
