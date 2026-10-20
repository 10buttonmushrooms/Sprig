#include <bundle_forcer.h>

#include <algorithm>
#include <cerrno>
#include <cstddef>
#include <cstdio>
#include <mutex>
#include <string>
#include <string_view>
#include <unordered_map>
#include <unordered_set>
#include <vector>

#include <dirent.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>

#include <jni.h>
#include <android/asset_manager.h>
#include <android/asset_manager_jni.h>

#include <BNM/Class.hpp>
#include <BNM/MethodBase.hpp>
#include <BNM/Utils.hpp>

#include <bloom/android.h>
#include "log.h"

// Ported from Rogue-Heroes' forced English bundle (mod/src/overrides.cpp), generalized to
// every bundle: files are extracted from the APK on first use and AssetBundle.LoadFromFile
// is redirected to them. The game's own cache and versions file are never modified.

namespace {

struct Bundle {
    std::string asset;
    std::string target;
    bool ready = false;
};

std::mutex g_mutex;
jobject g_assets = nullptr;
jmethodID g_list = nullptr;
AAssetManager *g_mgr = nullptr;
std::string g_files_dir;
std::string g_apk_stamp;
std::unordered_map<std::string, Bundle> g_bundles;
std::unordered_set<std::string> g_checked_modules;

using LoadFromFile_t = void *(*)(BNM::Structures::Mono::String *, void *);
LoadFromFile_t orig_LoadFromFile = nullptr;

struct ThreadEnv {
    JNIEnv *env = nullptr;
    bool attached = false;
    ThreadEnv() {
        JavaVM *vm = Bloom::JavaVm();
        if (!vm) return;
        if (vm->GetEnv(reinterpret_cast<void **>(&env), JNI_VERSION_1_6) == JNI_OK) return;
        env = nullptr;
        attached = vm->AttachCurrentThread(&env, nullptr) == JNI_OK;
        if (!attached) env = nullptr;
    }
    ~ThreadEnv() {
        if (attached) Bloom::JavaVm()->DetachCurrentThread();
    }
};

bool failed(JNIEnv *env) {
    if (!env->ExceptionCheck()) return false;
    env->ExceptionClear();
    return true;
}

std::string to_string(JNIEnv *env, jstring s) {
    if (!s) return {};
    const char *chars = env->GetStringUTFChars(s, nullptr);
    if (!chars) return {};
    std::string out(chars);
    env->ReleaseStringUTFChars(s, chars);
    return out;
}

jobject call_object(JNIEnv *env, jobject obj, const char *name, const char *sig) {
    if (!obj) return nullptr;
    jclass cls = env->GetObjectClass(obj);
    jmethodID m = env->GetMethodID(cls, name, sig);
    env->DeleteLocalRef(cls);
    if (failed(env) || !m) return nullptr;
    jobject r = env->CallObjectMethod(obj, m);
    return failed(env) ? nullptr : r;
}

std::string call_string(JNIEnv *env, jobject obj, const char *name, const char *sig) {
    auto s = static_cast<jstring>(call_object(env, obj, name, sig));
    std::string out = to_string(env, s);
    if (s) env->DeleteLocalRef(s);
    return out;
}

jobject current_application(JNIEnv *env) {
    jclass atClass = env->FindClass("android/app/ActivityThread");
    if (failed(env) || !atClass) return nullptr;
    jmethodID curApp =
        env->GetStaticMethodID(atClass, "currentApplication", "()Landroid/app/Application;");
    jobject app = failed(env) || !curApp ? nullptr : env->CallStaticObjectMethod(atClass, curApp);
    env->DeleteLocalRef(atClass);
    return failed(env) ? nullptr : app;
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

bool write_asset_atomic(AAsset *a, const std::string &target) {
    const auto slash = target.find_last_of('/');
    if (slash != std::string::npos) mkdir_p(target.substr(0, slash));
    std::string tmp = target + ".modtmp";
    FILE *f = std::fopen(tmp.c_str(), "wb");
    if (!f) return false;
    char buf[64 * 1024];
    bool ok = true;
    off64_t total = 0;
    int r;
    while ((r = AAsset_read(a, buf, sizeof(buf))) > 0) {
        if (std::fwrite(buf, 1, static_cast<std::size_t>(r), f) != static_cast<std::size_t>(r)) {
            ok = false;
            break;
        }
        total += r;
    }
    if (r < 0 || total != AAsset_getLength64(a)) ok = false;
    if (std::fclose(f) != 0) ok = false;

    if (!ok || std::rename(tmp.c_str(), target.c_str()) != 0) {
        std::remove(tmp.c_str());
        return false;
    }
    return true;
}

bool has_size(const std::string &path, off64_t size) {
    struct stat st{};
    return stat(path.c_str(), &st) == 0 && S_ISREG(st.st_mode) && st.st_size == size;
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

void list_assets(JNIEnv *env, const std::string &root, const std::string &rel,
                 std::vector<std::string> &out) {
    const std::string path = rel.empty() ? root : root + "/" + rel;
    jstring jpath = env->NewStringUTF(path.c_str());
    auto names = static_cast<jobjectArray>(env->CallObjectMethod(g_assets, g_list, jpath));
    env->DeleteLocalRef(jpath);
    if (failed(env) || !names) return;
    const jsize count = env->GetArrayLength(names);
    if (count == 0 && !rel.empty()) out.push_back(rel);
    for (jsize i = 0; i < count; ++i) {
        auto name = static_cast<jstring>(env->GetObjectArrayElement(names, i));
        std::string child = to_string(env, name);
        env->DeleteLocalRef(name);
        if (child.empty() || child[0] == '.') continue;
        list_assets(env, root, rel.empty() ? child : rel + "/" + child, out);
    }
    env->DeleteLocalRef(names);
}

bool prepare(JNIEnv *env) {
    jobject app = current_application(env);
    if (!app) return false;
    jobject assets = call_object(env, app, "getAssets", "()Landroid/content/res/AssetManager;");
    jobject filesDir = call_object(env, app, "getFilesDir", "()Ljava/io/File;");
    g_files_dir = call_string(env, filesDir, "getAbsolutePath", "()Ljava/lang/String;");
    const std::string apk = call_string(env, app, "getPackageCodePath", "()Ljava/lang/String;");
    if (filesDir) env->DeleteLocalRef(filesDir);
    env->DeleteLocalRef(app);
    if (!assets || g_files_dir.empty()) return false;

    jclass amClass = env->GetObjectClass(assets);
    g_list = env->GetMethodID(amClass, "list", "(Ljava/lang/String;)[Ljava/lang/String;");
    env->DeleteLocalRef(amClass);
    if (failed(env) || !g_list) return false;
    g_assets = env->NewGlobalRef(assets);
    env->DeleteLocalRef(assets);
    g_mgr = AAssetManager_fromJava(env, g_assets);

    struct stat st{};
    if (!apk.empty() && stat(apk.c_str(), &st) == 0) {
        g_apk_stamp = std::to_string(st.st_size) + ":" + std::to_string(st.st_mtim.tv_sec) + "." +
                      std::to_string(st.st_mtim.tv_nsec);
    }
    return g_mgr != nullptr;
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

bool extract(Bundle &bundle) {
    if (bundle.ready) return true;
    AAsset *a = AAssetManager_open(g_mgr, bundle.asset.c_str(), AASSET_MODE_STREAMING);
    if (!a) {
        LOG_ERR("BundleForcer: missing asset %s", bundle.asset.c_str());
        return false;
    }
    bundle.ready = has_size(bundle.target, AAsset_getLength64(a)) || write_asset_atomic(a, bundle.target);
    AAsset_close(a);
    if (!bundle.ready) LOG_ERR("BundleForcer: could not extract %s (errno %d)", bundle.target.c_str(), errno);
    return bundle.ready;
}

void *hooked_LoadFromFile(BNM::Structures::Mono::String *path, void *mi) {
    if (path) {
        const std::string original = path->str();
        std::string target;
        {
            std::lock_guard<std::mutex> lock(g_mutex);
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
    ThreadEnv thread;
    if (!thread.env) {
        LOG_ERR("BundleForcer: no JNIEnv for this thread");
        return false;
    }
    std::lock_guard<std::mutex> lock(g_mutex);
    if (!g_mgr && !prepare(thread.env)) {
        LOG_ERR("BundleForcer: Android assets are unavailable");
        return false;
    }

    const std::string name(module);
    const std::string dir(directory);
    const std::string root = g_files_dir + "/sprig/bloom/" + name;
    if (g_checked_modules.insert(name).second) refresh_extracted(root);

    const std::string assetDir = "sprig/bloom/" + name + "/" + dir;
    std::vector<std::string> files;
    list_assets(thread.env, assetDir, "", files);
    // Within one directory the highest version of a bundle wins.
    std::sort(files.begin(), files.end(), [](const std::string &a, const std::string &b) {
        long va, vb;
        const auto ka = split_version(a, &va), kb = split_version(b, &vb);
        return ka != kb ? ka < kb : va < vb;
    });
    std::size_t forced = 0;
    for (std::size_t i = 0; i < files.size(); ++i) {
        const std::string key = bundle_key(files[i]);
        if (i + 1 < files.size() && bundle_key(files[i + 1]) == key) {
            LOG_WARN("BundleForcer: %s/%s is shadowed by a higher version", assetDir.c_str(), files[i].c_str());
            continue;
        }
        g_bundles[key] = Bundle{assetDir + "/" + files[i], root + "/" + dir + "/" + files[i]};
        ++forced;
    }

    if (forced == 0) {
        LOG_INFO("BundleForcer: no bundles in %s", assetDir.c_str());
        return true;
    }
    if (!install_load_hook()) return false;
    LOG_INFO("BundleForcer: forcing %zu bundles from %s", forced, assetDir.c_str());
    return true;
}
