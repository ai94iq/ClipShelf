#!/bin/sh
# The build.bat twin: a self-contained publish for this machine's OS and CPU
# (linux-x64, osx-arm64, ...) in artifacts/publish/<rid>.
set -e
cd "$(dirname "$0")"
case "$(uname -s)" in
  Darwin) os=osx ;;
  *) os=linux ;;
esac
case "$(uname -m)" in
  arm64|aarch64) arch=arm64 ;;
  *) arch=x64 ;;
esac
RID=$os-$arch
OUT=artifacts/publish/$RID
rm -rf "$OUT"
dotnet publish src/Copyloft.Desktop/Copyloft.Desktop.csproj -c Release -r "$RID" --self-contained true \
  -p:PublishReadyToRun=true -o "$OUT" -v q -nologo
echo "[build] OK: $OUT/Copyloft"
