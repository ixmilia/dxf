#!/bin/sh -e

_SCRIPT_DIR="$( cd -P -- "$(dirname -- "$(command -v -- "$0")")" && pwd -P )"

# run code generator
GENERATOR_DIR=$_SCRIPT_DIR/src/IxMilia.Dxf.Generator
LIBRARY_DIR=$_SCRIPT_DIR/src/IxMilia.Dxf
cd $GENERATOR_DIR
dotnet restore
dotnet build
dotnet run "$LIBRARY_DIR"
cd -

# build library
LIBRARY_PROJECT=$LIBRARY_DIR/IxMilia.Dxf.csproj
dotnet restore "$LIBRARY_PROJECT"
dotnet build "$LIBRARY_PROJECT"

# build and run tests
TEST_PROJECT=$_SCRIPT_DIR/src/IxMilia.Dxf.Test/IxMilia.Dxf.Test.csproj
dotnet restore "$TEST_PROJECT"
dotnet build "$TEST_PROJECT"
dotnet test "$TEST_PROJECT"
