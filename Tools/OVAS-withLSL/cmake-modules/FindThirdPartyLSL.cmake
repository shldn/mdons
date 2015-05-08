# ---------------------------------
# Finds LIBLSL
# Adds library to target
# Adds include path
# ---------------------------------

FIND_PATH(PATH_LSL include/lsl_cpp.h PATHS $ENV{OV_DEP_LSL} $ENV{OpenViBE_dependencies} NO_DEFAULT_PATH)
IF(PATH_LSL)

	MESSAGE(STATUS "  Found LSL header...")
    INCLUDE_DIRECTORIES(${PATH_LSL}/include)
    LINK_DIRECTORIES(${PATH_LSL}/lib)

    # determine library name
	IF(APPLE)
        SET(LSL_EXTENSION ".dylib")
    ELSE(APPLE)
        IF(UNIX)
            SET(LSL_EXTENSION ".so")
        ELSE(UNIX)
            SET(LSL_EXTENSION ".dll")
        ENDIF(UNIX)
    ENDIF(APPLE)

    IF(CMAKE_SIZEOF_VOID_P EQUAL 8)
        SET(LSL_BITPOSTFIX "64")
    ELSE(CMAKE_SIZEOF_VOID_P EQUAL 8)
        SET(LSL_BITPOSTFIX "32")
    ENDIF(CMAKE_SIZEOF_VOID_P EQUAL 8)

	IF(CMAKE_BUILD_TYPE STREQUAL "Debug")
		SET(LSL_DEBUGPOSTFIX "") # NOTE: you may change this to "-debug" if you want to link to the debug version of the library in this case
	ELSE(CMAKE_BUILD_TYPE STREQUAL "Debug")
		SET(LSL_DEBUGPOSTFIX "")
	ENDIF(CMAKE_BUILD_TYPE STREQUAL "Debug")    
    
    FIND_LIBRARY(LIB_LSL liblsl${LSL_BITPOSTFIX}${LSL_DEBUGPOSTFIX} PATHS ${PATH_LSL}/lib NO_DEFAULT_PATH)
    IF(LIB_LSL)
        MESSAGE(STATUS "  [  OK  ] LSL library found...")    
    ELSE(LIB_LSL)
        MESSAGE(STATUS "  [FAILED] LSL library not found...")
    ENDIF(LIB_LSL)
        
    IF(LIB_LSL)
        # Link the library
		TARGET_LINK_LIBRARIES(${PROJECT_NAME}-dynamic ${LIB_LSL})        
		ADD_DEFINITIONS(-DTARGET_HAS_LIBLSL)

        # make sure that it gets copied into the bin directory...
		ADD_CUSTOM_COMMAND(
				TARGET ${PROJECT_NAME}-dynamic
				POST_BUILD
				COMMAND ${CMAKE_COMMAND}
				ARGS -E copy "${PATH_LSL}/lib/liblsl${LSL_BITPOSTFIX}${LSL_DEBUGPOSTFIX}${LSL_EXTENSION}" "${PROJECT_SOURCE_DIR}/bin"
				COMMENT "      --->   Copying library file ${PATH_LSL}/lib/liblsl${LSL_BITPOSTFIX}${LSL_DEBUGPOSTFIX}${LSL_EXTENSION} for LIBLSL."
			VERBATIM)
    ENDIF(LIB_LSL)
ELSE(PATH_LSL)
    MESSAGE(STATUS "  [FAILED] LSL header not found...")
ENDIF(PATH_LSL)
