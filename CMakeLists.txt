cmake_minimum_required(VERSION 3.0.0)
project(gc-progetto VERSION 0.1.0)

include(CTest)
enable_testing()

add_executable(gc-progetto 
    3rdparty/glad.c 
    3rdparty/stb_image.c 
    main_shadow.cpp)

target_link_libraries(gc-progetto GL dl glfw assimp)

target_include_directories(gc-progetto PUBLIC common/)

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)
