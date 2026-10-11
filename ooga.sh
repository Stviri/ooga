#!/bin/sh
# Run an ooga file on Linux or macOS:  ./ooga.sh examples/01_hello.ooga
# Needs the .NET 8 SDK (or newer). bin/ooga.exe is the Windows version (with windows and buttons);
# here ooga runs from its source, without windows.
exec dotnet run --project "$(dirname "$0")/engine/Ooga.Cli" -f net8.0 -v quiet -- "$@"
