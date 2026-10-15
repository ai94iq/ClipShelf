# Release checklist

1. Move CHANGELOG `Unreleased` entries under the new version heading with today's date, and add
   that heading to the CHANGELOG `Contents` list.
2. Bump `Version` in `Directory.Build.props` (MAJOR.MINOR.PATCH, always higher than the last shipped MSI).
3. Run `package.bat` locally and test the MSI on Windows 10 22H2 and on Windows 11, as a standard user:
   fresh install, upgrade from the previous MSI, launch, uninstall (user data must remain), downgrade
   blocked. On Linux run `package.sh` and install the `.deb` (Ubuntu) and the `.rpm` (Fedora) on a
   real desktop: launch, copy, the flyout from `clipshelf --toggle`, uninstall. On a Mac, if one is
   available, open the .dmg from CI and check the menu-bar icon, a copy and the shortcut.
4. Optional: sign `ClipShelf.exe` and the MSI. CI publishes the unsigned build.
5. Commit: `repo: build: release vX.Y.Z` with the changelog summary in the body.
6. Tag and push it: `git tag -a vX.Y.Z -m "vX.Y.Z"` then `git push origin vX.Y.Z`.
7. CI checks the tag against `Version`, runs `package.bat` and publishes a GitHub release with the MSI
   attached (`--generate-notes` builds the notes from the commit history); a second job adds the Linux
   tarball, `.deb` and `.rpm` built by `package.sh`, and a third the macOS `.dmg` files from
   `package-macos.sh`.
