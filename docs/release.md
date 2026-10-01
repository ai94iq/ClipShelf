# Release checklist

1. Move CHANGELOG `Unreleased` entries under the new version heading with today's date.
2. Bump `Version` in `Directory.Build.props` (MAJOR.MINOR.PATCH, always higher than the last shipped MSI).
3. Run `package.bat` locally and test the MSI on Windows 10 22H2 and on Windows 11, as a standard user:
   fresh install, upgrade from the previous MSI, launch, uninstall (user data must remain), downgrade
   blocked.
4. Optional: sign `ClipShelf.exe` and the MSI. CI publishes the unsigned build.
5. Commit: `repo: build: release vX.Y.Z` with the changelog summary in the body.
6. Tag and push it: `git tag -a vX.Y.Z -m "vX.Y.Z"` then `git push origin vX.Y.Z`.
7. CI checks the tag against `Version`, runs `package.bat` and publishes a GitHub release with the MSI
   attached (`--generate-notes` builds the notes from the commit history).
