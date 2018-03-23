@echo off

set PATHTOFILES=%1
set PROJECTFILE=IxMilia.Dxf.ReferenceCollector.csproj
set ARGS=
if not [%PATHTOFILES%] == [] set ARGS=-- "%PATHTOFILES%"

dotnet build "%~dp0%PROJECTFILE%"
dotnet run -p "%~dp0%PROJECTFILE%" %ARGS%
