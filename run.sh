#!/bin/sh
# The run.bat twin: runs the app in Debug.
set -e
cd "$(dirname "$0")"
dotnet run --project src/Copyloft.Desktop/Copyloft.Desktop.csproj -c Debug -v q
