#!/bin/sh
# Run an ooga file on Linux or macOS (needs .NET 8 or newer):  ./ooga.sh examples/01_hello.ooga
exec dotnet "$(dirname "$0")/bin/ooga.dll" "$@"
