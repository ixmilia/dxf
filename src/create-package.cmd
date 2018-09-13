@echo off

set PROJECT=%~dp0IxMilia.Dxf\IxMilia.Dxf.csproj
dotnet restore %PROJECT%
if errorlevel 1 exit /b 1
dotnet pack --configuration Release %PROJECT%
