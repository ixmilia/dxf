@echo off
setlocal

set thisdir=%~dp0

pushd "%thisdir%src\IxMilia.Dxf.Generator"
dotnet run -- "%thisdir%src\IxMilia.Dxf\Generated"
if errorlevel 1 goto error
popd

exit /b 0

:error
echo Error running generator.
popd
exit /b 1
