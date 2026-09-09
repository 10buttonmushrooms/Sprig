cmake_minimum_required(VERSION 3.22)
project(Sprig CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Dobby build options must be set BEFORE add_subdirectory.
set(DOBBY_BUILD_EXAMPLE             OFF CACHE BOOL "" FORCE)
set(DOBBY_BUILD_TEST                OFF CACHE BOOL "" FORCE)
set(DOBBY_DEBUG                     OFF CACHE BOOL "" FORCE)
set(Plugin.Android.BionicLinkerUtil ON  CACHE BOOL "" FORCE)

# BNM build options.
set(BNM_LINK_LOG OFF CACHE BOOL "" FORCE)

add_subdirectory(${CMAKE_SOURCE_DIR}/../deps/Dobby       ${CMAKE_BINARY_DIR}/Dobby-build)
add_subdirectory(${CMAKE_SOURCE_DIR}/../deps/BNM-Android ${CMAKE_BINARY_DIR}/BNM-build)

# BNM's GlobalSettings.hpp includes <dobby.h> but BNM's CMakeLists doesn't link
# Dobby, so dobby.h isn't on its include path. Fix it here.
target_include_directories(BNM PRIVATE ${CMAKE_SOURCE_DIR}/../deps/Dobby/include)

add_library(sprig SHARED
    src/main.cpp
)

target_include_directories(sprig PRIVATE
    include
    ${CMAKE_SOURCE_DIR}/../deps/BNM-Android/include
    ${CMAKE_SOURCE_DIR}/../deps/BNM-Android/external/include
)

target_link_libraries(sprig PRIVATE
    BNM
    dobby_static
    log
)

set_target_properties(sprig PROPERTIES
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON
)

target_compile_options(sprig PRIVATE
    -ffunction-sections
    -fdata-sections
)

target_link_options(sprig PRIVATE
    -Wl,--gc-sections
    -Wl,--strip-all
)
