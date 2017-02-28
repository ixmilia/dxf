#!/bin/sh

TEST_PROJECT=./src/IxMilia.Dxf.Test/IxMilia.Dxf.Test.csproj
dotnet restore $TEST_PROJECT
dotnet test $TEST_PROJECT

