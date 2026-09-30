#pragma once

#include <span>
#include <string_view>

namespace Bloom {
enum class Type { Core, PnP };
struct Module {
    std::string_view name;
    Type type;
    std::string_view config;
};

std::span<const Module> Modules();
const Module *FindModule(std::string_view name);
bool RegisterPnP(std::string_view name, void (*init)());
void InitializePnP();
std::string_view Config(std::string_view module, std::string_view section,
                        std::string_view key, std::string_view fallback = {});
bool ConfigBool(std::string_view module, std::string_view section, std::string_view key, bool fallback);
int ConfigInt(std::string_view module, std::string_view section, std::string_view key, int fallback);
}

// CMake supplies identity to each module's sources. Core code calls these APIs
// explicitly; only a PnP registration schedules work at Sprig startup.
#define BLOOM_CONFIG(section, key) ::Bloom::Config(BLOOM_MODULE_NAME, section, key)
#define BLOOM_CONFIG_BOOL(section, key, fallback) ::Bloom::ConfigBool(BLOOM_MODULE_NAME, section, key, fallback)
#define BLOOM_CONFIG_INT(section, key, fallback) ::Bloom::ConfigInt(BLOOM_MODULE_NAME, section, key, fallback)
#define BLOOM_DETAIL_JOIN_(a, b) a##b
#define BLOOM_DETAIL_JOIN(a, b) BLOOM_DETAIL_JOIN_(a, b)
#define BLOOM_REGISTER_PNP(init) \
    static_assert(BLOOM_MODULE_TYPE == ::Bloom::Type::PnP, "Only PnP modules can auto-initialize"); \
    namespace { [[maybe_unused]] const bool BLOOM_DETAIL_JOIN(bloom_registration_, __COUNTER__) = \
        ::Bloom::RegisterPnP(BLOOM_MODULE_NAME, init); }
