include("${CMAKE_CURRENT_LIST_DIR}/rule.cmake")
include("${CMAKE_CURRENT_LIST_DIR}/file.cmake")

set(autodrum_default_library_list )

# Handle files with suffix (s|as|asm|AS|ASM|As|aS|Asm), for group default-XC8
if(autodrum_default_default_XC8_FILE_TYPE_assemble)
add_library(autodrum_default_default_XC8_assemble OBJECT ${autodrum_default_default_XC8_FILE_TYPE_assemble})
    autodrum_default_default_XC8_assemble_rule(autodrum_default_default_XC8_assemble)
    list(APPEND autodrum_default_library_list "$<TARGET_OBJECTS:autodrum_default_default_XC8_assemble>")

endif()

# Handle files with suffix S, for group default-XC8
if(autodrum_default_default_XC8_FILE_TYPE_assemblePreprocess)
add_library(autodrum_default_default_XC8_assemblePreprocess OBJECT ${autodrum_default_default_XC8_FILE_TYPE_assemblePreprocess})
    autodrum_default_default_XC8_assemblePreprocess_rule(autodrum_default_default_XC8_assemblePreprocess)
    list(APPEND autodrum_default_library_list "$<TARGET_OBJECTS:autodrum_default_default_XC8_assemblePreprocess>")

endif()

# Handle files with suffix [cC], for group default-XC8
if(autodrum_default_default_XC8_FILE_TYPE_compile)
add_library(autodrum_default_default_XC8_compile OBJECT ${autodrum_default_default_XC8_FILE_TYPE_compile})
    autodrum_default_default_XC8_compile_rule(autodrum_default_default_XC8_compile)
    list(APPEND autodrum_default_library_list "$<TARGET_OBJECTS:autodrum_default_default_XC8_compile>")

endif()


# Main target for this project
add_executable(autodrum_default_image_wLTXyVhN ${autodrum_default_library_list})

set_target_properties(autodrum_default_image_wLTXyVhN PROPERTIES
    OUTPUT_NAME "default"
    SUFFIX ".elf"
    ADDITIONAL_CLEAN_FILES "${output_extensions}"
    RUNTIME_OUTPUT_DIRECTORY "${autodrum_default_output_dir}")
target_link_libraries(autodrum_default_image_wLTXyVhN PRIVATE ${autodrum_default_default_XC8_FILE_TYPE_link})

# Add the link options from the rule file.
autodrum_default_link_rule( autodrum_default_image_wLTXyVhN)


