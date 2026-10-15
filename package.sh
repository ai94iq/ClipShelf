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

dotnet publish src/Copyloft.Desktop/Copyloft.Desktop.csproj -c Release -r linux-x64 \
  --self-contained true -p:PublishReadyToRun=true -o "$PUBLISH" -v q -nologo

# The file system tree both packages install.
mkdir -p "$STAGE/opt/copyloft" "$STAGE/usr/bin" "$STAGE/usr/share/applications" \
  "$STAGE/usr/share/icons/hicolor/256x256/apps"
cp -a "$PUBLISH/." "$STAGE/opt/copyloft/"
ln -s /opt/copyloft/Copyloft "$STAGE/usr/bin/copyloft"
cp packaging/linux/copyloft.desktop "$STAGE/usr/share/applications/"
cp packaging/linux/copyloft.png "$STAGE/usr/share/icons/hicolor/256x256/apps/"

# Tarball: the app folder plus the desktop entry and icon, for any distribution.
TAR=copyloft-$VERSION
mkdir -p "$OUT/$TAR"
cp -a "$PUBLISH/." "$OUT/$TAR/"
cp packaging/linux/copyloft.desktop packaging/linux/copyloft.png "$OUT/$TAR/"
tar -C "$OUT" -czf "$OUT/copyloft-$VERSION-linux-x64.tar.gz" "$TAR"

# Debian and Ubuntu. ICU and OpenSSL are the .NET runtime's; dbus-send answers the tray check.
mkdir -p "$STAGE/DEBIAN"
cat > "$STAGE/DEBIAN/control" <<EOF
Package: copyloft
Version: $VERSION
Architecture: amd64
Maintainer: Copyloft <https://github.com/ai94iq/Copyloft>
Depends: libx11-6, libice6, libsm6, libxtst6, libxfixes3, libfontconfig1, libsecret-1-0,
 libicu72 | libicu74 | libicu76 | libicu77 | libicu78, libssl3 | libssl3t64, dbus-bin | dbus
Section: utils
Priority: optional
Homepage: https://github.com/ai94iq/Copyloft
Description: Clipboard history
 Copyloft keeps a searchable history of what you copy, encrypted on disk,
 and pastes it back.
EOF
dpkg-deb --root-owner-group --build "$STAGE" "$OUT/copyloft_${VERSION}_amd64.deb" >/dev/null
rm -rf "$STAGE/DEBIAN"

# Fedora and openSUSE.
rpmbuild -bb packaging/linux/copyloft.spec --quiet --nodeps \
  --define "_topdir $(pwd)/$OUT/rpmbuild" --define "version $VERSION" --define "stage $(pwd)/$STAGE"
cp "$OUT"/rpmbuild/RPMS/*/copyloft-*.rpm "$OUT/"

echo "[package] OK: $OUT"
ls -1 "$OUT"/*.tar.gz "$OUT"/*.deb "$OUT"/*.rpm
