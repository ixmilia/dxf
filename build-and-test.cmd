@echo off

:: run code generator
set GENERATOR_DIR=%~dp0src\IxMilia.Dxf.Generator
set LIBRARY_DIR=%~dp0src\IxMilia.Dxf
pushd "%GENERATOR_DIR%"
dotnet restore
if errorlevel 1 goto error
dotnet build
if errorlevel 1 goto error
dotnet run "%LIBRARY_DIR%"
if errorlevel 1 goto error
popd

:: build library
set LIBRARY_PROJECT=%LIBRARY_DIR%\IxMilia.Dxf.csproj
dotnet restore "%LIBRARY_PROJECT%"
if errorlevel 1 goto error
dotnet build "%LIBRARY_PROJECT%"
if errorlevel 1 goto error

:: build and run tests
set TEST_PROJECT=%~dp0src\IxMilia.Dxf.Test\IxMilia.Dxf.Test.csproj
dotnet restore "%TEST_PROJECT%"
if errorlevel 1 goto error
dotnet build "%TEST_PROJECT%"
if errorlevel 1 goto error
dotnet test "%TEST_PROJECT%"
if errorlevel 1 goto error
goto :eof

:error
echo Error building project.
exit /b 1
