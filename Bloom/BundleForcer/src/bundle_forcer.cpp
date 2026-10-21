#include <bundle_forcer.h>

#include <algorithm>
#include <cerrno>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <mutex>
#include <string>
#include <string_view>
#include <unordered_map>
#include <unordered_set>
#include <vector>

#include <dirent.h>
#include <dlfcn.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>
#include <zlib.h>

#include <BNM/Class.hpp>
#include <BNM/Method.hpp>
#include <BNM/MethodBase.hpp>
#include <BNM/Utils.hpp>

#include "log.h"

// Ported from Rogue-Heroes' forced English bundle (mod/src/overrides.cpp), generalized to
// every bundle. Forced files are read straight from the APK's zip, extracted on first use,
// and AssetBundle.LoadFromFile is redirected to them. The game's cache and versions file are
// never modified. Nothing here needs JNI or Sprig changes, so it also works under ARM
// translation (e.g. BlueStacks), where a host JavaVM cannot be called.

namespace {

struct ZipEntry {
    std::string name;
    std::uint16_t method = 0;
    std::uint64_t compressed = 0;
    std::uint64_t size = 0;
    std::uint64_t header = 0;
};

struct Bundle {
    ZipEntry entry;
    std::string target;
    bool ready = false;
};

struct Request {
    std::string module;
    std::string directory;
};

std::mutex g_mutex;
std::vector<Request> g_requests;
bool g_scanned = false;
bool g_ready = false;
std::string g_apk;
std::string g_data_dir;
std::string g_apk_stamp;
std::vector<ZipEntry> g_entries;
std::unordered_map<std::string, Bundle> g_bundles;
std::unordered_set<std::string> g_checked_modules;

using LoadFromFile_t = void *(*)(BNM::Structures::Mono::String *, void *);
LoadFromFile_t orig_LoadFromFile = nullptr;

std::uint16_t u16(const unsigned char *p) { return static_cast<std::uint16_t>(p[0] | (p[1] << 8)); }
std::uint32_t u32(const unsigned char *p) {
    return static_cast<std::uint32_t>(p[0]) | (static_cast<std::uint32_t>(p[1]) << 8) |
           (static_cast<std::uint32_t>(p[2]) << 16) | (static_cast<std::uint32_t>(p[3]) << 24);
}

void mkdir_p(const std::string &dir) {
    std::string cur;
    for (std::size_t i = 0; i < dir.size(); ++i) {
        cur += dir[i];
        if (dir[i] == '/' && cur.size() > 1) mkdir(cur.c_str(), 0770);
    }
    if (!dir.empty()) mkdir(dir.c_str(), 0770);
}

void remove_tree(const std::string &absDir) {
    DIR *d = opendir(absDir.c_str());
    if (!d) return;
    struct dirent *e;
    while ((e = readdir(d)) != nullptr) {
        const char *name = e->d_name;
        if (name[0] == '.' && (name[1] == '\0' || (name[1] == '.' && name[2] == '\0')))
            continue;
        std::string child = absDir + "/" + name;
        struct stat st{};
        if (lstat(child.c_str(), &st) != 0) continue;
        if (S_ISDIR(st.st_mode)) remove_tree(child);
        else std::remove(child.c_str());
    }
    closedir(d);
    rmdir(absDir.c_str());
}

std::string read_text(const std::string &path) {
    std::string out;
    FILE *f = std::fopen(path.c_str(), "rb");
    if (!f) return out;
    char buf[256];
    std::size_t r;
    while ((r = std::fread(buf, 1, sizeof(buf), f)) > 0) out.append(buf, r);
    std::fclose(f);
    return out;
}

bool write_text(const std::string &path, const std::string &text) {
    FILE *f = std::fopen(path.c_str(), "wb");
    if (!f) return false;
    bool ok = std::fwrite(text.data(), 1, text.size(), f) == text.size();
    return std::fclose(f) == 0 && ok;
}

bool has_size(const std::string &path, std::uint64_t size) {
    struct stat st{};
    return stat(path.c_str(), &st) == 0 && S_ISREG(st.st_mode) &&
           static_cast<std::uint64_t>(st.st_size) == size;
}

std::string unity_path(const char *getter) {
    BNM::Method<BNM::Structures::Mono::String *> method =
        BNM::Class("UnityEngine", "Application").GetMethod(getter, 0);
    if (!method.IsValid()) return {};
    auto *value = method();
    return value ? value->str() : std::string();
}

// Unity reports the APK as Application.dataPath. If it does not, libsprig.so was extracted
// to <install>/lib/<abi>/ beside <install>/base.apk.
std::string find_apk() {
    std::string apk = unity_path("get_dataPath");
    if (apk.size() > 4 && apk.compare(apk.size() - 4, 4, ".apk") == 0) return apk;
    Dl_info info{};
    if (dladdr(reinterpret_cast<void *>(&find_apk), &info) && info.dli_fname) {
        std::string lib = info.dli_fname;
        const auto libDir = lib.rfind("/lib/");
        if (libDir != std::string::npos) return lib.substr(0, libDir) + "/base.apk";
    }
    return {};
}

bool read_central_directory(const std::string &apk, std::vector<ZipEntry> &out) {
    FILE *f = std::fopen(apk.c_str(), "rb");
    if (!f) return false;
    bool ok = false;
    std::vector<unsigned char> tail;
    std::vector<unsigned char> cd;
    if (fseeko(f, 0, SEEK_END) == 0) {
        const off_t fileSize = ftello(f);
        const off_t tailSize = std::min<off_t>(fileSize, 22 + 0xFFFF);
        tail.resize(static_cast<std::size_t>(tailSize));
        if (tailSize >= 22 && fseeko(f, fileSize - tailSize, SEEK_SET) == 0 &&
            std::fread(tail.data(), 1, tail.size(), f) == tail.size()) {
            for (std::size_t i = tail.size() - 22 + 1; i-- > 0;) {
                if (u32(&tail[i]) != 0x06054b50) continue;
                const std::uint16_t count = u16(&tail[i + 10]);
                const std::uint32_t cdSize = u32(&tail[i + 12]);
                const std::uint32_t cdOffset = u32(&tail[i + 16]);
                if (count == 0xFFFF || cdSize == 0xFFFFFFFF || cdOffset == 0xFFFFFFFF) {
                    LOG_ERR("BundleForcer: zip64 APKs are not supported");
                    break;
                }
                cd.resize(cdSize);
                if (fseeko(f, cdOffset, SEEK_SET) != 0 || std::fread(cd.data(), 1, cd.size(), f) != cd.size())
                    break;
                std::size_t p = 0;
                for (std::uint16_t n = 0; n < count && p + 46 <= cd.size(); ++n) {
                    if (u32(&cd[p]) != 0x02014b50) break;
                    const std::uint16_t nameLen = u16(&cd[p + 28]);
                    const std::size_t next = p + 46 + nameLen + u16(&cd[p + 30]) + u16(&cd[p + 32]);
                    if (next > cd.size()) break;
                    ZipEntry entry;
                    entry.name.assign(reinterpret_cast<const char *>(&cd[p + 46]), nameLen);
                    entry.method = u16(&cd[p + 10]);
                    entry.compressed = u32(&cd[p + 20]);
                    entry.size = u32(&cd[p + 24]);
                    entry.header = u32(&cd[p + 42]);
                    if (entry.name.rfind("assets/sprig/bloom/", 0) == 0) out.push_back(std::move(entry));
                    p = next;
                }
                ok = true;
                break;
            }
        }
    }
    std::fclose(f);
    return ok;
}

struct Zlib {
    decltype(&inflateInit2_) init = nullptr;
    decltype(&inflate) run = nullptr;
    decltype(&inflateEnd) end = nullptr;
};

// libz.so is a public NDK library, but this module cannot add link flags to Sprig.
const Zlib *zlib() {
    static Zlib z;
    static const bool loaded = [] {
        void *lib = dlopen("libz.so", RTLD_NOW);
        if (!lib) return false;
        z.init = reinterpret_cast<decltype(z.init)>(dlsym(lib, "inflateInit2_"));
        z.run = reinterpret_cast<decltype(z.run)>(dlsym(lib, "inflate"));
        z.end = reinterpret_cast<decltype(z.end)>(dlsym(lib, "inflateEnd"));
        return z.init && z.run && z.end;
    }();
    return loaded ? &z : nullptr;
}

bool copy_entry(FILE *in, const ZipEntry &entry, FILE *out) {
    unsigned char header[30];
    if (fseeko(in, static_cast<off_t>(entry.header), SEEK_SET) != 0 ||
        std::fread(header, 1, sizeof(header), in) != sizeof(header) || u32(header) != 0x04034b50 ||
        fseeko(in, u16(&header[26]) + u16(&header[28]), SEEK_CUR) != 0) {
        return false;
    }
    std::vector<unsigned char> src(64 * 1024);
    std::uint64_t remaining = entry.compressed;
    if (entry.method == 0) {
        while (remaining > 0) {
            const std::size_t want = static_cast<std::size_t>(std::min<std::uint64_t>(remaining, src.size()));
            if (std::fread(src.data(), 1, want, in) != want || std::fwrite(src.data(), 1, want, out) != want)
                return false;
            remaining -= want;
        }
        return entry.compressed == entry.size;
    }
    const Zlib *z = entry.method == 8 ? zlib() : nullptr;
    if (!z) return false;
    std::vector<unsigned char> dst(256 * 1024);
    z_stream stream{};
    if (z->init(&stream, -MAX_WBITS, ZLIB_VERSION, sizeof(stream)) != Z_OK) return false;
    std::uint64_t written = 0;
    int status = Z_OK;
    while (status != Z_STREAM_END) {
        if (stream.avail_in == 0) {
            const std::size_t want = static_cast<std::size_t>(std::min<std::uint64_t>(remaining, src.size()));
            if (want == 0 || std::fread(src.data(), 1, want, in) != want) break;
            remaining -= want;
            stream.next_in = src.data();
            stream.avail_in = static_cast<uInt>(want);
        }
        stream.next_out = dst.data();
        stream.avail_out = static_cast<uInt>(dst.size());
        status = z->run(&stream, Z_NO_FLUSH);
        if (status != Z_OK && status != Z_STREAM_END) break;
        const std::size_t produced = dst.size() - stream.avail_out;
        if (std::fwrite(dst.data(), 1, produced, out) != produced) break;
        written += produced;
    }
    z->end(&stream);
    return status == Z_STREAM_END && written == entry.size;
}

bool extract(Bundle &bundle) {
    if (bundle.ready) return true;
    if (has_size(bundle.target, bundle.entry.size)) return bundle.ready = true;

    const auto slash = bundle.target.find_last_of('/');
    if (slash != std::string::npos) mkdir_p(bundle.target.substr(0, slash));
    const std::string tmp = bundle.target + ".modtmp";
    FILE *in = std::fopen(g_apk.c_str(), "rb");
    FILE *out = in ? std::fopen(tmp.c_str(), "wb") : nullptr;
    bool ok = in && out && copy_entry(in, bundle.entry, out);
    if (out && std::fclose(out) != 0) ok = false;
    if (in) std::fclose(in);
    if (!ok || std::rename(tmp.c_str(), bundle.target.c_str()) != 0) {
        std::remove(tmp.c_str());
        LOG_ERR("BundleForcer: could not extract %s (errno %d)", bundle.entry.name.c_str(), errno);
        return false;
    }
    return bundle.ready = true;
}

// Cached bundles are named <bundle>_<version>; forced files may carry any version or none.
std::string_view split_version(std::string_view rel, long *version) {
    *version = -1;
    const auto underscore = rel.rfind('_');
    const auto slash = rel.rfind('/');
    if (underscore == std::string_view::npos || underscore + 1 == rel.size() ||
        (slash != std::string_view::npos && underscore < slash) ||
        rel.find_first_not_of("0123456789", underscore + 1) != std::string_view::npos) {
        return rel;
    }
    *version = 0;
    for (char c : rel.substr(underscore + 1)) *version = std::min(*version * 10 + (c - '0'), 1L << 30);
    return rel.substr(0, underscore);
}

std::string bundle_key(std::string_view rel) {
    long version;
    return std::string(split_version(rel, &version));
}

// <persistentDataPath>/cache/bundles/{files,staged}/<bundle>_<version>
std::string cached_bundle_key(std::string_view path) {
    constexpr std::string_view kCache = "/cache/bundles/";
    const auto start = path.find(kCache);
    if (start == std::string_view::npos) return {};
    const auto slash = path.find('/', start + kCache.size());
    if (slash == std::string_view::npos) return {};
    return bundle_key(path.substr(slash + 1));
}

bool hidden(std::string_view rel) {
    return rel.empty() || rel.back() == '/' || rel.front() == '.' || rel.find("/.") != std::string_view::npos;
}

// A reinstalled APK may change a file without changing its size, so start over.
void refresh_extracted(const std::string &root) {
    if (g_apk_stamp.empty()) {
        LOG_WARN("BundleForcer: APK stamp unavailable; reusing %s", root.c_str());
        return;
    }
    const std::string stamp = root + "/.apk";
    if (read_text(stamp) == g_apk_stamp) return;
    remove_tree(root);
    mkdir_p(root);
    if (!write_text(stamp, g_apk_stamp)) LOG_WARN("BundleForcer: could not write %s", stamp.c_str());
}

void apply_request(const Request &request) {
    const std::string root = g_data_dir + "/sprig/bloom/" + request.module;
    if (g_checked_modules.insert(request.module).second) refresh_extracted(root);

    const std::string prefix = "assets/sprig/bloom/" + request.module + "/" + request.directory + "/";
    std::vector<const ZipEntry *> files;
    for (const auto &entry : g_entries) {
        if (entry.name.rfind(prefix, 0) == 0 && !hidden(std::string_view(entry.name).substr(prefix.size())))
            files.push_back(&entry);
    }
    // Within one directory the highest version of a bundle wins.
    std::sort(files.begin(), files.end(), [&](const ZipEntry *a, const ZipEntry *b) {
        long va, vb;
        const auto ka = split_version(std::string_view(a->name).substr(prefix.size()), &va);
        const auto kb = split_version(std::string_view(b->name).substr(prefix.size()), &vb);
        return ka != kb ? ka < kb : va < vb;
    });
    std::size_t forced = 0;
    for (std::size_t i = 0; i < files.size(); ++i) {
        const std::string rel = files[i]->name.substr(prefix.size());
        const std::string key = bundle_key(rel);
        if (i + 1 < files.size() && bundle_key(files[i + 1]->name.substr(prefix.size())) == key) {
            LOG_WARN("BundleForcer: %s is shadowed by a higher version", files[i]->name.c_str());
            continue;
        }
        g_bundles[key] = Bundle{*files[i], root + "/" + request.directory + "/" + rel};
        ++forced;
    }
    if (forced == 0) LOG_INFO("BundleForcer: no bundles in %s", prefix.c_str());
    else LOG_INFO("BundleForcer: forcing %zu bundles from %s", forced, prefix.c_str());
}

// Runs on the first bundle load, when Unity can report its paths.
void scan() {
    g_scanned = true;
    g_apk = find_apk();
    g_data_dir = unity_path("get_persistentDataPath");
    struct stat st{};
    if (g_apk.empty() || stat(g_apk.c_str(), &st) != 0 || g_data_dir.empty() ||
        !read_central_directory(g_apk, g_entries)) {
        LOG_ERR("BundleForcer: could not read APK '%s' or data path '%s'", g_apk.c_str(), g_data_dir.c_str());
        return;
    }
    g_apk_stamp = std::to_string(st.st_size) + ":" + std::to_string(st.st_mtim.tv_sec) + "." +
                  std::to_string(st.st_mtim.tv_nsec);
    g_ready = true;
    for (const auto &request : g_requests) apply_request(request);
}

void *hooked_LoadFromFile(BNM::Structures::Mono::String *path, void *mi) {
    if (path) {
        const std::string original = path->str();
        std::string target;
        {
            std::lock_guard<std::mutex> lock(g_mutex);
            if (!g_scanned) scan();
            auto it = g_bundles.find(cached_bundle_key(original));
            if (it != g_bundles.end()) {
                const bool first = !it->second.ready;
                if (extract(it->second)) target = it->second.target;
                if (first && !target.empty()) LOG_INFO("BundleForcer: %s -> %s", original.c_str(), target.c_str());
            }
        }
        if (!target.empty()) {
            if (auto *replacement = BNM::CreateMonoString(target)) {
                return orig_LoadFromFile(replacement, mi);
            }
        }
    }
    return orig_LoadFromFile(path, mi);
}

bool install_load_hook() {
    if (orig_LoadFromFile) return true;
    auto load = BNM::Class("UnityEngine", "AssetBundle").GetMethod("LoadFromFile", 1);
    if (!load.IsValid()) {
        LOG_ERR("BundleForcer: AssetBundle.LoadFromFile was not found");
        return false;
    }
    BNM::BasicHook(load, hooked_LoadFromFile, orig_LoadFromFile);
    if (!orig_LoadFromFile) {
        LOG_ERR("BundleForcer: AssetBundle.LoadFromFile hook failed");
        return false;
    }
    return true;
}

}

bool BundleForcer::Force(std::string_view module, std::string_view directory) {
    std::lock_guard<std::mutex> lock(g_mutex);
    g_requests.push_back({std::string(module), std::string(directory)});
    if (g_ready) apply_request(g_requests.back());
    return install_load_hook();
}
