#!/bin/sh -e

_SCRIPT_DIR="$( cd -P -- "$(dirname -- "$(command -v -- "$0")")" && pwd -P )"

cd "$_SCRIPT_DIR/src/IxMilia.Dxf.Generator"
dotnet run -- "$_SCRIPT_DIR/src/IxMilia.Dxf/Generated"
cd -
