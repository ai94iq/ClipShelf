# Release checklist

1. Move CHANGELOG `Unreleased` entries under the new version heading with today's date.
2. Bump `Version` in `Directory.Build.props` (MAJOR.MINOR.PATCH, always higher than the last shipped MSI).
3. Run `package.bat`.
4. Sign `ClipShelf.exe`, then rebuild and sign the MSI.
5. Test on a Windows 10 22H2 VM and on Windows 11, as a standard user: fresh install, upgrade from the previous MSI, launch, uninstall (user data must remain), downgrade blocked.
6. Commit: `repo: build: release vX.Y.Z` with the changelog summary in the body.
7. Tag: `git tag -a vX.Y.Z -m "vX.Y.Z"`.
