#!/bin/sh -e

_SCRIPT_DIR="$( cd -P -- "$(dirname -- "$(command -v -- "$0")")" && pwd -P )"
PROJECT=$_SCRIPT_DIR/IxMilia.Dxf/IxMilia.Dxf.csproj
dotnet restore $PROJECT
dotnet pack --include-symbols --include-source --configuration Release $PROJECT /p:OfficialBuild=true
