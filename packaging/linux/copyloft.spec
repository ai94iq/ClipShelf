# Wraps the staged tree package.sh builds; nothing is compiled here.
# The app is self-contained .NET, so automatic dependency scanning and stripping are off.
%global debug_package %{nil}
%global __os_install_post %{nil}

Name:           copyloft
Version:        %{version}
Release:        1
Summary:        Clipboard history
License:        MIT
URL:            https://github.com/ai94iq/Copyloft
AutoReqProv:    no
Requires:       libX11, libICE, libSM, libXtst, libXfixes, fontconfig, libsecret, libicu, openssl-libs, dbus-tools

%description
Copyloft keeps a searchable history of what you copy, encrypted on disk, and pastes it back.

%install
cp -a %{stage}/. %{buildroot}/

%files
/opt/copyloft
/usr/bin/copyloft
/usr/share/applications/copyloft.desktop
/usr/share/icons/hicolor/256x256/apps/copyloft.png
