# Publishing to winget

The manifests in `packaging/winget/` (schema 1.6) are templates: the release workflow rewrites
them on its runner for each tag and submits the result, so the copies in the repository keep an
older version. The flow:

1. On a `v*` tag, the workflow builds the MSI, publishes the release, then rewrites the three
   manifests on the runner (version, installer URL, SHA256 and product code read from the MSI).
2. If the repository secret `WINGET_TOKEN` exists, it installs `wingetcreate` and opens a pull
   request against [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).
3. winget's validation runs on the pull request; once it is merged,
   `winget install ai94iq.Copyloft` works.

## One-time setup

Create a classic GitHub token with the `public_repo` scope and store it as the repository secret
`WINGET_TOKEN` (Settings > Secrets and variables > Actions > New repository secret). Without the
secret the workflow skips the submission and only refreshes the manifests on the runner.

## Manual submission

When the secret is not set (or the submission needs a retry):

1. Download the MSI attached to the GitHub release and compute its SHA256.
2. Read the MSI's product code (Windows Installer COM, or let `wingetcreate update` do both).
3. Update `PackageVersion`, `InstallerUrl`, `InstallerSha256`, `ProductCode` and `ReleaseNotesUrl`
   in the three files.
4. Submit, either with [wingetcreate](https://github.com/microsoft/winget-create):

   ```
   wingetcreate submit --prtitle "New version: ai94iq.Copyloft version X.Y.Z" --token <GitHub token> packaging\winget
   ```

   or by hand: fork the repository, copy the three files into
   `manifests/a/ai94iq/Copyloft/<version>/` and open a pull request.
