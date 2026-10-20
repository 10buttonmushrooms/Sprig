#pragma once

#include <string_view>

namespace BundleForcer {
// Loads each file staged under assets/sprig/bloom/<module>/<directory>/ in place of the
// game's cached bundle with the same relative path, whatever version the game cached.
// A trailing _<version> on a file name is ignored. A later call wins for the same bundle.
bool Force(std::string_view module, std::string_view directory);
}
