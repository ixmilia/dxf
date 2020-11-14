#!/bin/sh -e

_SCRIPT_DIR="$( cd -P -- "$(dirname -- "$(command -v -- "$0")")" && pwd -P )"

CONFIGURATION=Debug
RUNTESTS=true

while [ $# -gt 0 ]; do
  case "$1" in
    --configuration|-c)
      CONFIGURATION=$2
      shift
      ;;
    --notest)
      RUNTESTS=false
      ;;
    *)
      echo "Invalid argument: $1"
      exit 1
      ;;
  esac
  shift
done

# run code generator
GENERATOR_DIR=$_SCRIPT_DIR/src/IxMilia.Dxf.Generator
LIBRARY_DIR=$_SCRIPT_DIR/src/IxMilia.Dxf
cd $GENERATOR_DIR
dotnet restore
dotnet build --configuration $CONFIGURATION
dotnet run --configuration $CONFIGURATION --no-restore --no-build -- "$LIBRARY_DIR"
cd -

# build
dotnet restore
dotnet build --configuration $CONFIGURATION

# run tests
if [ "$RUNTESTS" = "true" ]; then
  dotnet test --configuration $CONFIGURATION --no-restore --no-build
fi

# create packages
dotnet pack --no-restore --no-build --configuration $CONFIGURATION
PACKAGE_DIR="$_SCRIPT_DIR/artifacts/packages/$CONFIGURATION"
PACKAGE_COUNT=$(ls "$PACKAGE_DIR"/*.nupkg | wc -l)
if [ "$PACKAGE_COUNT" -ne "1" ]; then
  echo "Expected a single NuGet package but found $PACKAGE_COUNT at '$PACKAGE_DIR'"
  exit 1
fi
