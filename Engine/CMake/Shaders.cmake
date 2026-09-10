if(APPLE)
    set(SHADER_PLATFORM osx)
    set(SHADER_PROFILE metal)
else()
    set(SHADER_PLATFORM windows)
    set(SHADER_PROFILE s_5_0)
endif()
set(SHADER_OUTPUTS)
foreach(shader vs_scene fs_scene vs_inspector fs_inspector vs_fullscreen fs_display fs_calibration)
    if(shader MATCHES "^vs_")
        set(SHADER_TYPE vertex)
    else()
        set(SHADER_TYPE fragment)
    endif()
    set(input "${CMAKE_CURRENT_SOURCE_DIR}/Shaders/${shader}.sc")
    set(output "${CMAKE_CURRENT_BINARY_DIR}/Shaders/${shader}.bin")
    add_custom_command(OUTPUT "${output}"
        COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_BINARY_DIR}/Shaders"
        COMMAND $<TARGET_FILE:shaderc> -f "${input}" -o "${output}" --type ${SHADER_TYPE}
            --platform ${SHADER_PLATFORM} -p ${SHADER_PROFILE}
            --varyingdef "${CMAKE_CURRENT_SOURCE_DIR}/Shaders/varying.def.sc"
            -i "${BGFX_DIR}/src"
        DEPENDS shaderc "${input}" "${CMAKE_CURRENT_SOURCE_DIR}/Shaders/varying.def.sc"
            "${BGFX_DIR}/src/bgfx_shader.sh" "${BGFX_DIR}/src/bgfx_compute.sh"
        VERBATIM)
    list(APPEND SHADER_OUTPUTS "${output}")
endforeach()
add_custom_target(engine_shaders DEPENDS ${SHADER_OUTPUTS})
