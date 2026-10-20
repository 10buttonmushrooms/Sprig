#pragma once

#include <jni.h>

namespace Bloom {
// Sprig's process VM from JNI_OnLoad. Threads without a JNIEnv must attach first.
JavaVM *JavaVm();
}
