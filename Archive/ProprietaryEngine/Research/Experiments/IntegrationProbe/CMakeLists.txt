cmake_minimum_required(VERSION 3.20)
project(EngineIntegrationProbe LANGUAGES C CXX)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# An isolated compatibility probe. This is not the production engine build.
get_filename_component(ENGINE_ROOT "${CMAKE_CURRENT_LIST_DIR}/../../.." ABSOLUTE)
set(PROBE_SOURCES "${ENGINE_ROOT}/.cache/probe-sources")
if(NOT EXISTS "${PROBE_SOURCES}/bgfx_cmake/CMakeLists.txt")
    message(FATAL_ERROR "Run Tools/prepare_probe.py first")
endif()
set(SDL_SHARED OFF CACHE BOOL "" FORCE)
set(SDL_STATIC ON CACHE BOOL "" FORCE)
set(SDL_TESTS OFF CACHE BOOL "" FORCE)
set(SDL_TEST_LIBRARY OFF CACHE BOOL "" FORCE)
set(SDL_INSTALL OFF CACHE BOOL "" FORCE)
add_subdirectory("${PROBE_SOURCES}/sdl" "${CMAKE_BINARY_DIR}/sdl")

set(BGFX_DIR "${PROBE_SOURCES}/bgfx" CACHE STRING "" FORCE)
set(BX_DIR "${PROBE_SOURCES}/bx" CACHE STRING "" FORCE)
set(BIMG_DIR "${PROBE_SOURCES}/bimg" CACHE STRING "" FORCE)
set(BGFX_BUILD_TOOLS OFF CACHE BOOL "" FORCE)
set(BGFX_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BGFX_INSTALL OFF CACHE BOOL "" FORCE)
add_subdirectory("${PROBE_SOURCES}/bgfx_cmake" "${CMAKE_BINARY_DIR}/bgfx")

# The bundled bgfx copy needs its custom example configuration/support code.
# This CPU-only inspector check uses the independently pinned official release.
set(IMGUI_DIR "${PROBE_SOURCES}/imgui")
add_library(probe_imgui STATIC
    "${IMGUI_DIR}/imgui.cpp" "${IMGUI_DIR}/imgui_draw.cpp"
    "${IMGUI_DIR}/imgui_tables.cpp" "${IMGUI_DIR}/imgui_widgets.cpp")
target_include_directories(probe_imgui PUBLIC "${IMGUI_DIR}")
add_executable(integration_probe main.cpp)
target_link_libraries(integration_probe PRIVATE SDL3::SDL3-static bgfx probe_imgui)
enable_testing()
add_test(NAME headless_integration COMMAND integration_probe)
