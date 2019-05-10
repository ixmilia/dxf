@echo off
setlocal

set configuration=Debug
set runtests=true

:parseargs
if "%1" == "" goto argsdone
if /i "%1" == "-c" (
    set configuration=%2
    shift
    shift
    goto parseargs
)
if /i "%1" == "-notest" (
    set runtests=false
    shift
    goto parseargs
)

echo Unsupported argument: %1
goto error

:argsdone

:: run code generator
set GENERATOR_DIR=%~dp0src\IxMilia.Dxf.Generator
set LIBRARY_DIR=%~dp0src\IxMilia.Dxf
pushd "%GENERATOR_DIR%"
dotnet restore
if errorlevel 1 goto error
dotnet build -c %configuration%
if errorlevel 1 goto error
dotnet run -c %configuration% --no-restore --no-build -- "%LIBRARY_DIR%"
if errorlevel 1 goto error
popd

:: build library
set LIBRARY_PROJECT=%LIBRARY_DIR%\IxMilia.Dxf.csproj
dotnet restore "%LIBRARY_PROJECT%"
if errorlevel 1 goto error
dotnet build "%LIBRARY_PROJECT%" -c %configuration%
if errorlevel 1 goto error

:: build tests
set TEST_PROJECT=%~dp0src\IxMilia.Dxf.Test\IxMilia.Dxf.Test.csproj
dotnet restore "%TEST_PROJECT%"
if errorlevel 1 goto error
dotnet build "%TEST_PROJECT%" -c %configuration%
if errorlevel 1 goto error

:: run tests
if /i "%runtests%" == "true" (
    dotnet test "%TEST_PROJECT%" -c %configuration% --no-restore --no-build
    if errorlevel 1 goto error
)

goto :eof

:error
echo Error building project.
exit /b 1
