#!/bin/sh
# The test.bat twin. The X11 tests need a display; without one they skip (CI runs under xvfb-run).
set -e
cd "$(dirname "$0")"
dotnet test --solution ClipShelf.slnx -c Release
echo "[test] OK"
