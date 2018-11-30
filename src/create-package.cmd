@echo off

set PROJECT_NAME=IxMilia.Dxf
set CONFIGURATION=Release
set PROJECT=%~dp0\%PROJECT_NAME%\%PROJECT_NAME%.csproj
set OUTPUT_PATH=%~dp0..\Artifacts\NuGet

dotnet restore "%PROJECT%"
if errorlevel 1 exit /b 1

dotnet build "%PROJECT%" --configuration %CONFIGURATION%
if errorlevel 1 exit /b 1

dotnet pack --no-restore --no-build --configuration %CONFIGURATION% --output "%OUTPUT_PATH%" "%PROJECT%"
if errorlevel 1 exit /b 1
