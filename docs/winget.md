# Publishing to winget

The manifests for the current release live in `packaging/winget/` (schema 1.6).

For each new release:

1. Download the MSI attached to the GitHub release and compute its SHA256.
2. Read the MSI's product code (Windows Installer COM, or let `wingetcreate update` do both).
3. Update `PackageVersion`, `InstallerUrl`, `InstallerSha256`, `ProductCode` and `ReleaseNotesUrl`
   in the three files.
4. Submit to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs), either with
   [wingetcreate](https://github.com/microsoft/winget-create):

   ```
   wingetcreate submit --token <GitHub token with public_repo> packaging\winget
   ```

   or by hand: fork the repository, copy the three files into
   `manifests/a/ai94iq/ClipShelf/<version>/` and open a pull request.
5. winget's validation runs on the pull request; once it is merged,
   `winget install ai94iq.ClipShelf` works.
