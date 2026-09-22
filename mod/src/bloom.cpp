#include "bloom/bloom.hpp"

#include <algorithm>
#include <charconv>
#include <string_view>
#include <system_error>
#include <vector>

#include "log.h"

namespace sprig::bloom::detail {
std::string_view EmbeddedConfigFor(std::string_view module);
}

namespace sprig::bloom {
namespace {

struct Module {
    std::string_view name;
    InitFunction init;
};

std::vector<Module> &Modules() {
    static std::vector<Module> modules;
    return modules;
}

bool &Initialized() {
    static bool initialized = false;
    return initialized;
}

std::string_view Trim(std::string_view value) {
    while (!value.empty()) {
        const char c = value.front();
        if (c != ' ' && c != '\t' && c != '\r' && c != '\n') break;
        value.remove_prefix(1);
    }
    while (!value.empty()) {
        const char c = value.back();
        if (c != ' ' && c != '\t' && c != '\r' && c != '\n') break;
        value.remove_suffix(1);
    }
    return value;
}

char AsciiLower(char c) {
    if (c >= 'A' && c <= 'Z') return static_cast<char>(c + ('a' - 'A'));
    return c;
}

bool EqualsIgnoreCase(std::string_view left, std::string_view right) {
    if (left.size() != right.size()) return false;
    for (std::size_t i = 0; i < left.size(); ++i) {
        if (AsciiLower(left[i]) != AsciiLower(right[i])) return false;
    }
    return true;
}

} // namespace

bool RegisterPnP(std::string_view module, InitFunction init) {
    if (Initialized() || module.empty() || init == nullptr) return false;

    auto &modules = Modules();
    const auto duplicate = std::find_if(
        modules.begin(),
        modules.end(),
        [module](const Module &candidate) { return candidate.name == module; }
    );
    if (duplicate != modules.end()) return false;

    modules.push_back({module, init});
    return true;
}

void InitializePnP() {
    if (Initialized()) return;
    Initialized() = true;

    auto &modules = Modules();
    std::sort(
        modules.begin(),
        modules.end(),
        [](const Module &left, const Module &right) { return left.name < right.name; }
    );

    for (const Module &module : modules) {
        LOG_INFO("Bloom: loading %.*s", static_cast<int>(module.name.size()), module.name.data());
        module.init();
    }
}

std::string_view ConfigValue(
    std::string_view module,
    std::string_view section,
    std::string_view key
) {
    const std::string_view text = detail::EmbeddedConfigFor(module);
    if (text.empty() || key.empty()) return {};

    bool inSection = section.empty();
    std::size_t offset = 0;

    while (offset <= text.size()) {
        std::size_t end = text.find('\n', offset);
        if (end == std::string_view::npos) end = text.size();

        std::string_view line = Trim(text.substr(offset, end - offset));
        if (!line.empty() && line.front() != '#' && line.front() != ';') {
            if (line.size() >= 2 && line.front() == '[' && line.back() == ']') {
                const std::string_view currentSection = Trim(line.substr(1, line.size() - 2));
                inSection = currentSection == section;
            } else if (inSection) {
                const std::size_t equals = line.find('=');
                if (equals != std::string_view::npos) {
                    const std::string_view currentKey = Trim(line.substr(0, equals));
                    if (currentKey == key) {
                        return Trim(line.substr(equals + 1));
                    }
                }
            }
        }

        if (end == text.size()) break;
        offset = end + 1;
    }

    return {};
}

bool ConfigBool(
    std::string_view module,
    std::string_view section,
    std::string_view key,
    bool fallback
) {
    const std::string_view value = ConfigValue(module, section, key);
    if (value.empty()) return fallback;

    if (value == "1" || EqualsIgnoreCase(value, "true") ||
        EqualsIgnoreCase(value, "yes") || EqualsIgnoreCase(value, "on")) {
        return true;
    }
    if (value == "0" || EqualsIgnoreCase(value, "false") ||
        EqualsIgnoreCase(value, "no") || EqualsIgnoreCase(value, "off")) {
        return false;
    }
    return fallback;
}

int ConfigInt(
    std::string_view module,
    std::string_view section,
    std::string_view key,
    int fallback
) {
    const std::string_view value = ConfigValue(module, section, key);
    if (value.empty()) return fallback;

    int parsed = 0;
    const char *begin = value.data();
    const char *end = value.data() + value.size();
    const auto result = std::from_chars(begin, end, parsed);
    if (result.ec != std::errc{} || result.ptr != end) return fallback;
    return parsed;
}

} // namespace sprig::bloom
