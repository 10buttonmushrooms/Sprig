function(_bloom_manifest_value manifest key out_var)
    file(STRINGS "${manifest}" _lines)
    set(_value "")
    foreach(_line IN LISTS _lines)
        string(STRIP "${_line}" _line)
        if(_line MATCHES "^${key}[ \t]*=[ \t]*(.+)$")
            set(_value "${CMAKE_MATCH_1}")
            string(STRIP "${_value}" _value)
            break()
        endif()
    endforeach()
    set(${out_var} "${_value}" PARENT_SCOPE)
endfunction()

function(_bloom_generate_config_registry target)
    get_property(_names GLOBAL PROPERTY SPRIG_BLOOM_CONFIG_NAMES)
    get_property(_paths GLOBAL PROPERTY SPRIG_BLOOM_CONFIG_PATHS)

    set(_generated_dir "${CMAKE_BINARY_DIR}/bloom-generated")
    set(_generated_file "${_generated_dir}/configs.cpp")
    file(MAKE_DIRECTORY "${_generated_dir}")

    set(_source "#include <string_view>\n\nnamespace sprig::bloom::detail {\nstd::string_view EmbeddedConfigFor(std::string_view module) {\n")

    list(LENGTH _names _count)
    if(_count GREATER 0)
        math(EXPR _last "${_count} - 1")
        foreach(_index RANGE 0 ${_last})
            list(GET _names ${_index} _name)
            list(GET _paths ${_index} _path)
            file(READ "${_path}" _config)
            string(MD5 _hash "${_config}")
            string(SUBSTRING "${_hash}" 0 12 _short_hash)
            set(_delimiter "B${_short_hash}")
            string(APPEND _source "    if (module == \"${_name}\") return R\"${_delimiter}(${_config})${_delimiter}\";\n")
        endforeach()
    endif()

    string(APPEND _source "    return {};\n}\n} // namespace sprig::bloom::detail\n")
    file(WRITE "${_generated_file}" "${_source}")
    target_sources(${target} PRIVATE "${_generated_file}")
endfunction()

function(bloom_discover_modules target bloom_root)
    if(NOT TARGET ${target})
        message(FATAL_ERROR "Bloom target does not exist: ${target}")
    endif()

    set_property(GLOBAL PROPERTY SPRIG_BLOOM_CONFIG_NAMES "")
    set_property(GLOBAL PROPERTY SPRIG_BLOOM_CONFIG_PATHS "")

    if(NOT EXISTS "${bloom_root}")
        message(STATUS "Bloom: no module directory at ${bloom_root}")
        _bloom_generate_config_registry(${target})
        return()
    endif()

    file(GLOB _entries LIST_DIRECTORIES true CONFIGURE_DEPENDS "${bloom_root}/*")
    foreach(_module_dir IN LISTS _entries)
        if(NOT IS_DIRECTORY "${_module_dir}")
            continue()
        endif()

        get_filename_component(_module_id "${_module_dir}" NAME)
        if(_module_id MATCHES "^[._]")
            continue()
        endif()
        if(NOT _module_id MATCHES "^[A-Za-z0-9][A-Za-z0-9_.-]*$")
            message(FATAL_ERROR "Bloom module directory has an invalid id: ${_module_id}")
        endif()

        set(_manifest "${_module_dir}/bloom.ini")
        if(NOT EXISTS "${_manifest}")
            message(STATUS "Bloom: skipping ${_module_id} (no bloom.ini)")
            continue()
        endif()
        set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${_manifest}")

        _bloom_manifest_value("${_manifest}" "type" _type)
        string(TOLOWER "${_type}" _type)
        if(NOT _type STREQUAL "core" AND NOT _type STREQUAL "pnp")
            message(FATAL_ERROR "Bloom module ${_module_id} must set type=core or type=pnp in bloom.ini")
        endif()

        file(GLOB_RECURSE _module_sources CONFIGURE_DEPENDS
            "${_module_dir}/src/*.cpp"
            "${_module_dir}/src/*.cc"
            "${_module_dir}/src/*.cxx"
        )
        if(EXISTS "${_module_dir}/main.cpp")
            list(PREPEND _module_sources "${_module_dir}/main.cpp")
        endif()

        if(_type STREQUAL "pnp")
            if(NOT EXISTS "${_module_dir}/main.cpp")
                message(FATAL_ERROR "Bloom PnP module ${_module_id} requires main.cpp")
            endif()

            set(_config "${_module_dir}/config.ini")
            if(NOT EXISTS "${_config}")
                message(FATAL_ERROR "Bloom PnP module ${_module_id} requires config.ini")
            endif()
            set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${_config}")
            set_property(GLOBAL APPEND PROPERTY SPRIG_BLOOM_CONFIG_NAMES "${_module_id}")
            set_property(GLOBAL APPEND PROPERTY SPRIG_BLOOM_CONFIG_PATHS "${_config}")
        endif()

        if(_module_sources)
            target_sources(${target} PRIVATE ${_module_sources})
            foreach(_source IN LISTS _module_sources)
                set_property(
                    SOURCE "${_source}"
                    APPEND
                    PROPERTY COMPILE_DEFINITIONS "BLOOM_MODULE_ID=\"${_module_id}\""
                )
            endforeach()
        endif()

        if(EXISTS "${_module_dir}/inc")
            target_include_directories(${target} PRIVATE "${_module_dir}/inc")
        endif()
        if(EXISTS "${_module_dir}/include")
            target_include_directories(${target} PRIVATE "${_module_dir}/include")
        endif()

        message(STATUS "Bloom: ${_module_id} [${_type}]")
    endforeach()

    _bloom_generate_config_registry(${target})
endfunction()
