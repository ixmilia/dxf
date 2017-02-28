#!/bin/sh

PROJECT=./IxMilia.Dxf/IxMilia.Dxf.csproj
dotnet restore $PROJECT
dotnet pack --include-symbols --include-source --configuration Release $PROJECT

