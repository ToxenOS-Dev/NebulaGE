cmake_minimum_required(VERSION 3.25)
project(NebulaEngine VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Export compile commands for IDE integration
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# ── Output directories ───────────────────────────────────────
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# ── Dependencies (stubs — fill in as subsystems are added) ───
# find_package(Vulkan REQUIRED)

# ── Engine library ───────────────────────────────────────────
add_library(NebulaEngine SHARED
    # src/core/engine.cpp        (uncomment as files are added)
    # src/renderer/renderer.cpp
    # src/physics/physics.cpp
    # src/audio/audio.cpp
)

target_include_directories(NebulaEngine
    PUBLIC  ${CMAKE_CURRENT_SOURCE_DIR}/include
    PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/src
)

# target_link_libraries(NebulaEngine PRIVATE Vulkan::Vulkan)

# ── Compiler flags ───────────────────────────────────────────
target_compile_options(NebulaEngine PRIVATE
    -Wall -Wextra -Wpedantic
    $<$<CONFIG:Release>:-O3 -march=native>
    $<$<CONFIG:Debug>:-g -fsanitize=address,undefined>
)

target_link_options(NebulaEngine PRIVATE
    $<$<CONFIG:Debug>:-fsanitize=address,undefined>
)

message(STATUS "NebulaEngine ${PROJECT_VERSION} configured")
message(STATUS "  Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "  C++ standard: ${CMAKE_CXX_STANDARD}")
