@echo off

set PROJECT_NAME=IxMilia.Dxf
set CONFIGURATION=Release
set PROJECT=%~dp0\%PROJECT_NAME%\%PROJECT_NAME%.csproj
set /p VERSION=<"%~dp0..\version.txt"
set PACKAGE_FILE=%~dp0..\Artifacts\%PROJECT_NAME%\bin\%CONFIGURATION%\%PROJECT_NAME%.%VERSION%.nupkg
set FINAL_PACKAGE_FILE=%~dp0..\Artifacts\%PROJECT_NAME%\bin\%CONFIGURATION%\%PROJECT_NAME%.nupkg

del "%PACKAGE_FILE%"
del "%FINAL_PACKAGE_FILE%"

dotnet restore %PROJECT%
if errorlevel 1 exit /b 1

dotnet pack --configuration %CONFIGURATION% %PROJECT%
if errorlevel 1 exit /b 1

copy "%PACKAGE_FILE%" "%FINAL_PACKAGE_FILE%"
