#!/bin/sh
# The package.bat twin for Linux: a self-contained publish, then a tarball, a .deb and an .rpm in
# artifacts/linux. Needs dotnet, dpkg-deb and rpmbuild.
set -e
cd "$(dirname "$0")"
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
OUT=artifacts/linux
PUBLISH=$OUT/publish
STAGE=$OUT/stage
rm -rf "$OUT"

dotnet publish src/ClipShelf.Desktop/ClipShelf.Desktop.csproj -c Release -r linux-x64 \
  --self-contained true -p:PublishReadyToRun=true -o "$PUBLISH" -v q -nologo

# The file system tree both packages install.
mkdir -p "$STAGE/opt/clipshelf" "$STAGE/usr/bin" "$STAGE/usr/share/applications" \
  "$STAGE/usr/share/icons/hicolor/256x256/apps"
cp -a "$PUBLISH/." "$STAGE/opt/clipshelf/"
ln -s /opt/clipshelf/ClipShelf "$STAGE/usr/bin/clipshelf"
cp packaging/linux/clipshelf.desktop "$STAGE/usr/share/applications/"
cp packaging/linux/clipshelf.png "$STAGE/usr/share/icons/hicolor/256x256/apps/"

# Tarball: the app folder plus the desktop entry and icon, for any distribution.
TAR=clipshelf-$VERSION
mkdir -p "$OUT/$TAR"
cp -a "$PUBLISH/." "$OUT/$TAR/"
cp packaging/linux/clipshelf.desktop packaging/linux/clipshelf.png "$OUT/$TAR/"
tar -C "$OUT" -czf "$OUT/clipshelf-$VERSION-linux-x64.tar.gz" "$TAR"

# Debian and Ubuntu. ICU and OpenSSL are the .NET runtime's; dbus-send answers the tray check.
mkdir -p "$STAGE/DEBIAN"
cat > "$STAGE/DEBIAN/control" <<EOF
Package: clipshelf
Version: $VERSION
Architecture: amd64
Maintainer: ClipShelf <https://github.com/ai94iq/ClipShelf>
Depends: libx11-6, libice6, libsm6, libxtst6, libxfixes3, libfontconfig1, libsecret-1-0,
 libicu72 | libicu74 | libicu76 | libicu77 | libicu78, libssl3 | libssl3t64, dbus-bin | dbus
Section: utils
Priority: optional
Homepage: https://github.com/ai94iq/ClipShelf
Description: Clipboard history
 ClipShelf keeps a searchable history of what you copy, encrypted on disk,
 and pastes it back.
EOF
dpkg-deb --root-owner-group --build "$STAGE" "$OUT/clipshelf_${VERSION}_amd64.deb" >/dev/null
rm -rf "$STAGE/DEBIAN"

# Fedora and openSUSE.
rpmbuild -bb packaging/linux/clipshelf.spec --quiet --nodeps \
  --define "_topdir $(pwd)/$OUT/rpmbuild" --define "version $VERSION" --define "stage $(pwd)/$STAGE"
cp "$OUT"/rpmbuild/RPMS/*/clipshelf-*.rpm "$OUT/"

echo "[package] OK: $OUT"
ls -1 "$OUT"/*.tar.gz "$OUT"/*.deb "$OUT"/*.rpm
