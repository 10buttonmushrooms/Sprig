#include <bloom/bloom.h>

#include <atomic>
#include <charconv>
#include <cstdio>
#include <string>
#include <vector>
#ifdef __ANDROID__
#include "log.h"
#endif

namespace {
struct Initializer { std::string_view name; void (*init)(); };
auto &Initializers() {
    static std::vector<Initializer> entries;
    return entries;
}
std::string_view Trim(std::string_view value) {
    const auto start = value.find_first_not_of(" \t\r\n");
    if (start == value.npos) return {};
    return value.substr(start, value.find_last_not_of(" \t\r\n") - start + 1);
}
}

namespace Bloom {
const Module *FindModule(std::string_view name) {
    for (const auto &module : Modules()) if (module.name == name) return &module;
    return nullptr;
}

bool RegisterPnP(std::string_view name, void (*init)()) {
    if (!init) return false;
    for (const auto &entry : Initializers())
        if (entry.name == name && entry.init == init) return false;
    Initializers().push_back({name, init});
    return true;
}

void InitializePnP() {
    static std::atomic<bool> started{false};
    // Mark before callbacks so recursive and repeated loaded events cannot rerun them.
    if (started.exchange(true)) return;
    const auto entries = Initializers();
    for (const auto &entry : entries) {
        const auto *module = FindModule(entry.name);
        if (!module || module->type != Type::PnP) continue;
        try { entry.init(); }
        catch (...) {
#ifdef __ANDROID__
            LOG_ERR("Bloom initializer failed: %.*s", static_cast<int>(entry.name.size()), entry.name.data());
#else
            std::fprintf(stderr, "Bloom initializer failed: %.*s\n", static_cast<int>(entry.name.size()), entry.name.data());
#endif
        }
    }
}

std::string_view Config(std::string_view name, std::string_view section,
                        std::string_view key, std::string_view fallback) {
    const auto *module = FindModule(name);
    if (!module) return fallback;
    auto text = module->config;
    if (text.starts_with("\xef\xbb\xbf")) text.remove_prefix(3);
    std::string_view currentSection;
    auto value = fallback;
    // ponytail: scan small embedded configs per lookup; cache only if profiling warrants it.
    while (!text.empty()) {
        const auto end = text.find('\n');
        auto line = Trim(text.substr(0, end));
        text = end == text.npos ? std::string_view{} : text.substr(end + 1);
        if (line.empty() || line.front() == ';' || line.front() == '#') continue;
        if (line.front() == '[' && line.back() == ']') {
            currentSection = Trim(line.substr(1, line.size() - 2));
            continue;
        }
        const auto equals = line.find('=');
        if (currentSection == section && equals != line.npos && Trim(line.substr(0, equals)) == key)
            value = Trim(line.substr(equals + 1));
    }
    return value;
}

bool ConfigBool(std::string_view name, std::string_view section, std::string_view key, bool fallback) {
    std::string value(Config(name, section, key));
    for (char &c : value) if (c >= 'A' && c <= 'Z') c += 'a' - 'A';
    if (value == "true" || value == "yes" || value == "on" || value == "1") return true;
    if (value == "false" || value == "no" || value == "off" || value == "0") return false;
    return fallback;
}

int ConfigInt(std::string_view name, std::string_view section, std::string_view key, int fallback) {
    auto value = Config(name, section, key);
    if (value.starts_with('+')) {
        value.remove_prefix(1);
        if (value.starts_with('-')) return fallback;
    }
    if (value.empty()) return fallback;
    int result = 0;
    const auto parsed = std::from_chars(value.data(), value.data() + value.size(), result);
    return parsed.ec == std::errc{} && parsed.ptr == value.data() + value.size() ? result : fallback;
}
}
