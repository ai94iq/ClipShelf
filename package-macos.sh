#!/bin/sh
# Builds ClipShelf.app for Apple Silicon and Intel and a .dmg for each in artifacts/macos. Runs on
# macOS (sips, iconutil, codesign, hdiutil). The bundle is ad-hoc signed, not notarized: Apple
# Silicon refuses unsigned code, and there is no Developer ID (README: first launch via Open).
set -e
cd "$(dirname "$0")"
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
OUT=artifacts/macos
rm -rf "$OUT"
mkdir -p "$OUT"

# The icon set from the 256 px PNG the Linux packages use.
ICONSET="$OUT/clipshelf.iconset"
mkdir -p "$ICONSET"
icon() { sips -z "$1" "$1" packaging/linux/clipshelf.png --out "$ICONSET/$2" >/dev/null; }
icon 16 icon_16x16.png
icon 32 icon_16x16@2x.png
icon 32 icon_32x32.png
icon 64 icon_32x32@2x.png
icon 128 icon_128x128.png
icon 256 icon_128x128@2x.png
icon 256 icon_256x256.png
iconutil -c icns "$ICONSET" -o "$OUT/clipshelf.icns"

for rid in osx-arm64 osx-x64; do
  APP="$OUT/$rid/ClipShelf.app"
  mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
  dotnet publish src/ClipShelf.Desktop/ClipShelf.Desktop.csproj -c Release -r $rid \
    --self-contained true -o "$APP/Contents/MacOS" -v q -nologo
  sed "s/@VERSION@/$VERSION/g" packaging/macos/Info.plist > "$APP/Contents/Info.plist"
  cp "$OUT/clipshelf.icns" "$APP/Contents/Resources/"
  codesign --force --deep --sign - "$APP"

  # Structural checks: a valid plist, a signature that verifies, an executable where the plist
  # says it is.
  plutil -lint "$APP/Contents/Info.plist"
  codesign --verify --deep --strict "$APP"
  test -x "$APP/Contents/MacOS/ClipShelf"

  arch=${rid#osx-}
  hdiutil create -quiet -volname ClipShelf -srcfolder "$APP" -ov -format UDZO "$OUT/ClipShelf-$VERSION-macos-$arch.dmg"
done

echo "[package] OK: $OUT"
ls -1 "$OUT"/*.dmg
