#!/bin/sh
# The run.bat twin: runs the app in Debug.
set -e
cd "$(dirname "$0")"
dotnet run --project src/ClipShelf.Desktop/ClipShelf.Desktop.csproj -c Debug -v q
