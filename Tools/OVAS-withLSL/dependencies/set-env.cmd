@echo off

SET OV_DEP_ITPP=%CD%\dependencies\itpp
SET OV_DEP_EXPAT=%CD%\dependencies\expat
SET OV_DEP_GTK=%CD%\dependencies\gtk
SET OV_DEP_ITPP=%CD%\dependencies\itpp
SET OV_DEP_OGRE=%CD%\dependencies\ogre
SET OV_DEP_CEGUI=%CD%\dependencies\cegui
SET OV_DEP_VRPN=%CD%\dependencies\vrpn
SET OV_DEP_LUA=%CD%\dependencies\lua
SET OV_DEP_OPENAL=%CD%\dependencies\openal
SET OV_DEP_FREEALUT=%CD%\dependencies\freealut
SET OV_DEP_LIBVORBIS=%CD%\dependencies\libvorbis
SET OV_DEP_LIBOGG=%CD%\dependencies\libogg

SET OGRE_HOME=%CD%\dependencies\ogre
SET VRPNROOT=%CD%\dependencies\vrpn
SET OV_DEP_PTHREADS=%CD%\dependencies\pthreads

SET PATH=%OV_DEP_LUA%\lib;%PATH%
SET PATH=%OV_DEP_ITPP%\bin;%PATH%
SET PATH=%OV_DEP_EXPAT%\bin;%PATH%
SET PATH=%OV_DEP_GTK%\bin;%PATH%
SET PATH=%OV_DEP_ITPP%\bin;%PATH%
SET PATH=%OV_DEP_CEGUI%\bin;%PATH%
SET PATH=%OV_DEP_OGRE%\bin\release;%OV_DEP_OGRE%\bin\debug;%PATH%
SET PATH=%OV_DEP_VRPN%\bin;%PATH%
SET PATH=%OV_DEP_PTHREADS%\lib;%PATH%
SET PATH=%OV_DEP_OPENAL%\libs\Win32;%PATH%
SET PATH=%OV_DEP_FREEALUT%\lib;%PATH%
SET PATH=%OV_DEP_LIBVORBIS%\win32\bin\release;%PATH%
SET PATH=%OV_DEP_LIBOGG%\win32\bin\release\;%PATH%
