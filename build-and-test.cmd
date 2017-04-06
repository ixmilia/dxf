set TEST_PROJECT=.\src\IxMilia.Dxf.Test\IxMilia.Dxf.Test.csproj
dotnet restore %TEST_PROJECT%
if errorlevel 1 exit /b 1
dotnet test %TEST_PROJECT%

