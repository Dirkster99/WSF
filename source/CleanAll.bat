@ECHO OFF
pushd "%~dp0"
ECHO.
ECHO.
ECHO.
ECHO This script deletes all temporary build files in their
ECHO corresponding BIN and OBJ Folder contained in the following projects
ECHO.
ECHO WSF
ECHO.
ECHO Demos\Client
ECHO Demos\PerformanceTestClient
ECHO Demos\UnitTestWSF
ECHO Demos\WpfPerformance
ECHO.
REM Ask the user if hes really sure to continue beyond this point XXXXXXXX
set /p choice=Are you sure to continue (Y/N)?
if not '%choice%'=='Y' Goto EndOfBatch
REM Script does not continue unless user types 'Y' in upper case letter
ECHO.
ECHO XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
ECHO.
ECHO XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

RMDIR /S /Q .vs
RMDIR /S /Q TestResults

ECHO.
ECHO Deleting BIN and OBJ Folders in BmLib folder
ECHO.
RMDIR /S /Q WSF\bin
RMDIR /S /Q WSF\obj

ECHO.
ECHO Deleting BIN and OBJ Folders in Client folder
ECHO.
RMDIR /S /Q Demos\Client\bin
RMDIR /S /Q Demos\Client\obj

ECHO Deleting BIN and OBJ Folders in Demos\PerformanceTestClient folder
ECHO.
RMDIR /S /Q Demos\PerformanceTestClient\bin
RMDIR /S /Q Demos\PerformanceTestClient\obj

ECHO.
ECHO Deleting BIN and OBJ Folders in UnitTests folder
ECHO.
RMDIR /S /Q Demos\UnitTestWSF\bin
RMDIR /S /Q Demos\UnitTestWSF\obj

ECHO Deleting BIN and OBJ Folders in WpfPerformance folder
ECHO.
RMDIR /S /Q Demos\WpfPerformance\bin
RMDIR /S /Q Demos\WpfPerformance\obj

PAUSE

:EndOfBatch