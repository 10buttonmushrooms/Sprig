#include <jni.h>

#include <BNM/Class.hpp>
#include <BNM/Loading.hpp>
#include <BNM/MethodBase.hpp>
#include <BNM/Method.hpp>
#include <BNM/Utils.hpp>

#include "log.h"

using FailsMinimumAppVersion = bool (*)(void *, const void *);
static FailsMinimumAppVersion originalFailsMinimumAppVersion = nullptr;

using FindAssociation = void *(*)(void *, BNM::Structures::Mono::String *, const void *);
static FindAssociation originalFindAssociation = nullptr;
static BNM::Method<void *> findLatestAssociation;

static bool BlockUpdate(void *, const void *) { return false; }

static void *UseLatestContent(void *self, BNM::Structures::Mono::String *, const void *) {
    LOG_INFO("Latest content association selected");
    return findLatestAssociation[self](BNM::CreateMonoString("2147483647.2147483647.2147483647.2147483647"));
}

static void OnIl2CppLoaded() {
    auto versionMap = BNM::Class("PvZCards.Game", "AssetVersionMap");
    findLatestAssociation = versionMap.GetMethod("FindLastGoodAssociation", 1);
    if (!findLatestAssociation.IsValid()) {
        LOG_ERR("AssetVersionMap lookup methods were not found");
        return;
    }
    BNM::BasicHook(versionMap.GetMethod("FindAssociation", 1), UseLatestContent, originalFindAssociation);
    if (!originalFindAssociation) {
        LOG_ERR("AssetVersionMap.FindAssociation hook failed");
        return;
    }

    auto minimumAppVersion = BNM::Class("PvZCards.Application", "EnforceMinimumAppVersionCommand")
        .GetMethod("FailsMinimumAppVersion", 0);
    if (!minimumAppVersion.IsValid()) {
        LOG_ERR("EnforceMinimumAppVersionCommand.FailsMinimumAppVersion was not found");
        return;
    }

    BNM::BasicHook(minimumAppVersion, BlockUpdate, originalFailsMinimumAppVersion);
    if (!originalFailsMinimumAppVersion) {
        LOG_ERR("EnforceMinimumAppVersionCommand.FailsMinimumAppVersion hook failed");
        return;
    }

    LOG_INFO("Sprig loaded");
}

extern "C" JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM *vm, void * /*reserved*/) {
    JNIEnv *env = nullptr;
    if (vm->GetEnv(reinterpret_cast<void **>(&env), JNI_VERSION_1_6) != JNI_OK || env == nullptr) {
        LOG_ERR("JNI_OnLoad: GetEnv failed");
        return JNI_VERSION_1_6;
    }

    BNM::Loading::AllowLateInitHook();
    BNM::Loading::AddOnLoadedEvent(&OnIl2CppLoaded);

    if (!BNM::Loading::TryLoadByJNI(env)) {
        LOG_ERR("BNM TryLoadByJNI returned false (relying on late-init hook)");
    } else {
        LOG_INFO("BNM TryLoadByJNI ok; awaiting il2cpp_init");
    }

    return JNI_VERSION_1_6;
}
